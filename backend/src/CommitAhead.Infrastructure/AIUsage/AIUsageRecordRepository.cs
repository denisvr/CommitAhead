using CommitAhead.Application.AIUsage;
using CommitAhead.Domain.AIUsage;
using CommitAhead.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommitAhead.Infrastructure.AIUsage;

public sealed class AIUsageRecordRepository : IAIUsageRecordRepository
{
    private readonly CommitAheadDbContext _dbContext;

    public AIUsageRecordRepository(CommitAheadDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<AIUsageRecord?> GetByIdempotencyKeyAsync(Guid ownerUserId, string idempotencyKey, CancellationToken cancellationToken)
    {
        return _dbContext.AIUsageRecords
            .SingleOrDefaultAsync(record => record.OwnerUserId == ownerUserId && record.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task AddAsync(AIUsageRecord record, CancellationToken cancellationToken)
    {
        _dbContext.AIUsageRecords.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<decimal> GetSpentCostAsync(Guid ownerUserId, DateTime windowStartUtc, DateTime windowEndUtc, CancellationToken cancellationToken)
    {
        var records = await _dbContext.AIUsageRecords
            .Where(record =>
                record.OwnerUserId == ownerUserId
                && record.StartedAtUtc >= windowStartUtc
                && record.StartedAtUtc < windowEndUtc
                && (record.Status == AIUsageRecordStatus.Completed || record.Status == AIUsageRecordStatus.Reserved))
            .ToListAsync(cancellationToken);

        return records.Sum(record => record.Status == AIUsageRecordStatus.Completed ? record.ActualCost ?? 0 : record.ReservedCost);
    }
}
