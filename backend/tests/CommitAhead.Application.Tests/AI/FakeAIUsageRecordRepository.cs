using CommitAhead.Application.AI;
using CommitAhead.Application.AIUsage;
using CommitAhead.Domain.AIUsage;

namespace CommitAhead.Application.Tests.AI;

/// <summary>
/// Handwritten in-memory fake, per docs/testing/strategy.md Layer 2. Mirrors the real repository's
/// two unique constraints (per-owner idempotency key, at most one active Reserved record per
/// owner) by throwing the same AIUsageReservationConflictException on AddAsync, so
/// AnalyzeJobAnalysisUseCase's conflict-handling can be exercised without a real database.
/// </summary>
public sealed class FakeAIUsageRecordRepository : IAIUsageRecordRepository
{
    private readonly List<AIUsageRecord> _records = [];

    public IReadOnlyList<AIUsageRecord> Records => _records;

    public Task<AIUsageRecord?> GetByIdempotencyKeyAsync(Guid ownerUserId, string idempotencyKey, CancellationToken cancellationToken)
    {
        var record = _records.SingleOrDefault(r => r.OwnerUserId == ownerUserId && r.IdempotencyKey == idempotencyKey);
        return Task.FromResult(record);
    }

    public Task<AIUsageRecord?> GetActiveReservationByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken)
    {
        var record = _records.SingleOrDefault(r => r.OwnerUserId == ownerUserId && r.Status == AIUsageRecordStatus.Reserved);
        return Task.FromResult(record);
    }

    public Task AddAsync(AIUsageRecord record, CancellationToken cancellationToken)
    {
        var conflict = _records.Any(r => r.OwnerUserId == record.OwnerUserId && r.IdempotencyKey == record.IdempotencyKey)
            || _records.Any(r => r.OwnerUserId == record.OwnerUserId && r.Status == AIUsageRecordStatus.Reserved);

        if (conflict)
        {
            throw new AIUsageReservationConflictException();
        }

        _records.Add(record);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        // In-memory: mutations already apply directly to the tracked instance.
        return Task.CompletedTask;
    }

    public Task<decimal> GetSpentCostAsync(Guid ownerUserId, DateTime windowStartUtc, DateTime windowEndUtc, CancellationToken cancellationToken)
    {
        var records = _records.Where(r =>
            r.OwnerUserId == ownerUserId
            && r.StartedAtUtc >= windowStartUtc
            && r.StartedAtUtc < windowEndUtc
            && (r.Status == AIUsageRecordStatus.Completed || r.Status == AIUsageRecordStatus.Reserved));

        var spent = records.Sum(r => r.Status == AIUsageRecordStatus.Completed ? r.ActualCost ?? 0 : r.ReservedCost);
        return Task.FromResult(spent);
    }
}
