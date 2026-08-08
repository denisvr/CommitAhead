using CommitAhead.Domain.AIUsage;
using CommitAhead.Domain.AnalysisDrafts;
using CommitAhead.Domain.EvidenceLinks;
using CommitAhead.Infrastructure.AIUsage;
using CommitAhead.Infrastructure.AnalysisDrafts;
using CommitAhead.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

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
        var unitOfWork = new EfUnitOfWork(_dbContext, NullLogger<EfUnitOfWork>.Instance);

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

    /// <summary>
    /// Rollback uses its own short independent token — never the caller's, which may already be
    /// why the operation failed. Proves that even when the caller's own token is already cancelled
    /// by the time the operation throws, rollback still succeeds and the *original* exception
    /// propagates (never an OperationCanceledException from a rollback that never got a chance to
    /// run with a live token).
    /// </summary>
    [Fact]
    public async Task ExecuteInTransactionAsync_WhenTheCallerTokenIsAlreadyCancelled_StillRollsBackAndRethrowsTheOriginalException()
    {
        var unitOfWork = new EfUnitOfWork(_dbContext, NullLogger<EfUnitOfWork>.Instance);
        using var callerCts = new CancellationTokenSource();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => unitOfWork.ExecuteInTransactionAsync<bool>(
            _ =>
            {
                callerCts.Cancel();
                throw new InvalidOperationException("Simulated failure after the caller's own token was cancelled.");
            },
            callerCts.Token));

        Assert.Equal("Simulated failure after the caller's own token was cancelled.", exception.Message);
    }

    /// <summary>
    /// RlsTransactionActionFilter already wraps every [UsesOwnerScopedData] controller action in
    /// its own transaction for the whole action — ExecuteInTransactionAsync must nest inside an
    /// already-active transaction rather than attempting a second BeginTransactionAsync on the
    /// same connection, which Npgsql/EF Core rejects.
    /// </summary>
    [Fact]
    public async Task ExecuteInTransactionAsync_WithAnAlreadyActiveTransaction_NestsInsideItInsteadOfStartingASecondOne()
    {
        var unitOfWork = new EfUnitOfWork(_dbContext, NullLogger<EfUnitOfWork>.Instance);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var draftRepository = new AnalysisDraftRepository(_dbContext);
        var draftId = Guid.NewGuid();

        await using var ambientTransaction = await _dbContext.Database.BeginTransactionAsync();

        var result = await unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                var draft = new AnalysisDraft(draftId, ownerUserId, EvidenceSourceType.JobAnalysis, Guid.NewGuid(), [], [], [], DateTime.UtcNow);
                await draftRepository.AddAsync(draft, ct);
                return true;
            },
            CancellationToken.None);

        Assert.True(result);
        await ambientTransaction.CommitAsync();

        await using var reloadDbContext = new CommitAheadDbContext(new DbContextOptionsBuilder<CommitAheadDbContext>().UseNpgsql(_fixture.ConnectionString).Options);
        Assert.NotNull(await new AnalysisDraftRepository(reloadDbContext).GetByIdAsync(ownerUserId, draftId, CancellationToken.None));
    }
}
