using CommitAhead.Application.AI;
using CommitAhead.Application.AIUsage;
using CommitAhead.Domain.AIUsage;
using CommitAhead.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CommitAhead.Infrastructure.AIUsage;

public sealed class AIUsageRecordRepository : IAIUsageRecordRepository
{
    // https://www.postgresql.org/docs/current/errcodes-appendix.html — unique_violation.
    private const string PostgresUniqueViolationSqlState = "23505";

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

    public Task<AIUsageRecord?> GetActiveReservationByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken)
    {
        return _dbContext.AIUsageRecords
            .SingleOrDefaultAsync(record => record.OwnerUserId == ownerUserId && record.Status == AIUsageRecordStatus.Reserved, cancellationToken);
    }

    public async Task AddAsync(AIUsageRecord record, CancellationToken cancellationToken)
    {
        _dbContext.AIUsageRecords.Add(record);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresUniqueViolationSqlState })
        {
            throw new AIUsageReservationConflictException();
        }
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
