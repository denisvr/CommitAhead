using CommitAhead.Domain.AIUsage;
using CommitAhead.Domain.EvidenceLinks;
using CommitAhead.Infrastructure.AIUsage;
using CommitAhead.Infrastructure.Persistence;
using CommitAhead.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommitAhead.Infrastructure.Tests.AIUsage;

[Collection(PostgresCollection.Name)]
public sealed class AIUsageRecordRepositoryTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private CommitAheadDbContext _dbContext = null!;

    public AIUsageRecordRepositoryTests(PostgresContainerFixture fixture)
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

    private static AIUsageRecord CreateRecord(Guid ownerUserId, string idempotencyKey, DateTime startedAtUtc) => new(
        Guid.NewGuid(), ownerUserId, idempotencyKey, AiCommandType.AnalyzeJobAnalysis, EvidenceSourceType.JobAnalysis, Guid.NewGuid(),
        "anthropic", "claude-fake", "2026-01-01", "usd", 1000, 500, 0.05m, startedAtUtc);

    [Fact]
    public async Task AddThenGetByIdempotencyKey_RoundTripsTheReservation()
    {
        var repository = new AIUsageRecordRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var record = CreateRecord(ownerUserId, "key-1", DateTime.UtcNow);

        await repository.AddAsync(record, CancellationToken.None);

        await using var reloadDbContext = new CommitAheadDbContext(new DbContextOptionsBuilder<CommitAheadDbContext>().UseNpgsql(_fixture.ConnectionString).Options);
        var reloaded = await new AIUsageRecordRepository(reloadDbContext).GetByIdempotencyKeyAsync(ownerUserId, "key-1", CancellationToken.None);

        Assert.NotNull(reloaded);
        Assert.Equal(AIUsageRecordStatus.Reserved, reloaded.Status);
        Assert.Equal("USD", reloaded.Currency);
    }

    [Fact]
    public async Task GetByIdempotencyKeyAsync_ReturnsNull_ForADifferentOwner()
    {
        var repository = new AIUsageRecordRepository(_dbContext);
        var ownerAId = await TestUsers.CreateAsync(_dbContext);
        var ownerBId = await TestUsers.CreateAsync(_dbContext);
        await repository.AddAsync(CreateRecord(ownerAId, "key-1", DateTime.UtcNow), CancellationToken.None);

        var found = await repository.GetByIdempotencyKeyAsync(ownerBId, "key-1", CancellationToken.None);

        Assert.Null(found);
    }

    /// <summary>Proves the database's own unique constraint on idempotency_key (ADR-0014's durable idempotency), not just an application-level check.</summary>
    [Fact]
    public async Task SaveChanges_WithADuplicateIdempotencyKey_ThrowsFromTheDatabaseConstraint()
    {
        var repository = new AIUsageRecordRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        await repository.AddAsync(CreateRecord(ownerUserId, "duplicate-key", DateTime.UtcNow), CancellationToken.None);

        _dbContext.AIUsageRecords.Add(CreateRecord(ownerUserId, "duplicate-key", DateTime.UtcNow));

        await Assert.ThrowsAsync<DbUpdateException>(() => _dbContext.SaveChangesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Complete_PersistsActualUsageAndAnalysisDraftId()
    {
        var repository = new AIUsageRecordRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var record = CreateRecord(ownerUserId, "key-1", DateTime.UtcNow);
        await repository.AddAsync(record, CancellationToken.None);
        var analysisDraftId = Guid.NewGuid();

        record.Complete(900, 450, 0.045m, analysisDraftId, "success", DateTime.UtcNow);
        await repository.SaveChangesAsync(CancellationToken.None);

        await using var reloadDbContext = new CommitAheadDbContext(new DbContextOptionsBuilder<CommitAheadDbContext>().UseNpgsql(_fixture.ConnectionString).Options);
        var reloaded = await new AIUsageRecordRepository(reloadDbContext).GetByIdempotencyKeyAsync(ownerUserId, "key-1", CancellationToken.None);

        Assert.Equal(AIUsageRecordStatus.Completed, reloaded!.Status);
        Assert.Equal(analysisDraftId, reloaded.AnalysisDraftId);
        Assert.Equal(0.045m, reloaded.ActualCost);
    }

    [Fact]
    public async Task GetSpentCostAsync_SumsCompletedActualCostPlusActiveReservedCost_WithinTheWindow()
    {
        var repository = new AIUsageRecordRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var windowStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var windowEnd = windowStart.AddDays(1);

        var completed = CreateRecord(ownerUserId, "completed-key", windowStart.AddHours(1));
        completed.Complete(900, 450, 0.045m, Guid.NewGuid(), "success", windowStart.AddHours(1).AddSeconds(3));
        await repository.AddAsync(completed, CancellationToken.None);

        var reserved = CreateRecord(ownerUserId, "reserved-key", windowStart.AddHours(2));
        await repository.AddAsync(reserved, CancellationToken.None);

        var failed = CreateRecord(ownerUserId, "failed-key", windowStart.AddHours(3));
        failed.Fail("provider-timeout", windowStart.AddHours(3).AddSeconds(10));
        await repository.AddAsync(failed, CancellationToken.None);

        // Outside the window entirely — must not contribute.
        await repository.AddAsync(CreateRecord(ownerUserId, "outside-window-key", windowStart.AddDays(-1)), CancellationToken.None);

        var spent = await repository.GetSpentCostAsync(ownerUserId, windowStart, windowEnd, CancellationToken.None);

        // 0.045 (completed actual) + 0.05 (reserved, still active) — the failed record contributes nothing.
        Assert.Equal(0.095m, spent);
    }
}
