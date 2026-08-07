using CommitAhead.Domain.AIUsage;
using CommitAhead.Domain.AnalysisDrafts;
using CommitAhead.Domain.EvidenceLinks;
using CommitAhead.Infrastructure.AIUsage;
using CommitAhead.Infrastructure.AnalysisDrafts;
using CommitAhead.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommitAhead.Infrastructure.Tests.Persistence;

[Collection(PostgresCollection.Name)]
public sealed class EfUnitOfWorkTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private CommitAheadDbContext _dbContext = null!;

    public EfUnitOfWorkTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<CommitAheadDbContext>().UseNpgsql(_fixture.ConnectionString).Options;
        _dbContext = new CommitAheadDbContext(options);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _fixture.ResetAsync();
    }

    /// <summary>
    /// Proves the exact hazard AnalyzeJobAnalysisUseCase's failure handling guards against: after a
    /// forced failure inside the draft/completion transaction, the AnalysisDraft insert rolls back
    /// (no orphan), and — critically — a fresh read of the AIUsageRecord (after the rollback clears
    /// the change tracker) still shows Reserved, not the in-memory Completed value Complete() set
    /// before the rollback, so calling Fail() on it afterward succeeds rather than throwing.
    /// </summary>
    [Fact]
    public async Task ExecuteInTransactionAsync_WhenTheOperationThrows_RollsBackTheDraftAndLeavesTheUsageRecordReloadableAsReserved()
    {
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var usageRepository = new AIUsageRecordRepository(_dbContext);
        var draftRepository = new AnalysisDraftRepository(_dbContext);
        var unitOfWork = new EfUnitOfWork(_dbContext);

        var record = new AIUsageRecord(
            Guid.NewGuid(), ownerUserId, "key-1", AiCommandType.AnalyzeJobAnalysis, EvidenceSourceType.JobAnalysis, Guid.NewGuid(),
            "fake", "fake-test-model", "fake-v1", "USD", 1000, 500, 0m, DateTime.UtcNow);
        await usageRepository.AddAsync(record, CancellationToken.None);

        var draftId = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(() => unitOfWork.ExecuteInTransactionAsync<bool>(
            async ct =>
            {
                var draft = new AnalysisDraft(draftId, ownerUserId, EvidenceSourceType.JobAnalysis, Guid.NewGuid(), [], [], [], DateTime.UtcNow);
                await draftRepository.AddAsync(draft, ct);

                record.Complete(100, 50, 0m, draftId, "success", DateTime.UtcNow);
                await usageRepository.SaveChangesAsync(ct);

                throw new InvalidOperationException("Simulated downstream failure.");
            },
            CancellationToken.None));

        // No draft remains — the insert rolled back with everything else in the transaction.
        var reloadedDraft = await draftRepository.GetByIdAsync(ownerUserId, draftId, CancellationToken.None);
        Assert.Null(reloadedDraft);

        // The change tracker was cleared on rollback, so this re-read returns a fresh instance
        // reflecting the database's real (post-rollback) state — Reserved, not the aborted
        // Completed mutation — and Fail() on it succeeds rather than throwing.
        var freshRecord = await usageRepository.GetByIdempotencyKeyAsync(ownerUserId, "key-1", CancellationToken.None);
        Assert.Equal(AIUsageRecordStatus.Reserved, freshRecord!.Status);

        freshRecord.Fail("simulated-failure", DateTime.UtcNow);
        await usageRepository.SaveChangesAsync(CancellationToken.None);

        await using var reloadDbContext = new CommitAheadDbContext(new DbContextOptionsBuilder<CommitAheadDbContext>().UseNpgsql(_fixture.ConnectionString).Options);
        var finalRecord = await new AIUsageRecordRepository(reloadDbContext).GetByIdempotencyKeyAsync(ownerUserId, "key-1", CancellationToken.None);
        Assert.Equal(AIUsageRecordStatus.Failed, finalRecord!.Status);
    }
}
