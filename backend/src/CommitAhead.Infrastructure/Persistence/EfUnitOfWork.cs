using CommitAhead.Application.Persistence;

namespace CommitAhead.Infrastructure.Persistence;

/// <summary>
/// Wraps <c>operation</c> in one explicit database transaction. On failure, rolls back and then
/// calls <see cref="Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.Clear"/> before
/// rethrowing — the contract callers rely on: after a failed <see cref="ExecuteInTransactionAsync{T}"/>,
/// every entity touched during the failed attempt is detached, so a fresh repository query
/// afterward returns a clean instance reflecting the database's actual (post-rollback) state,
/// never a stale in-memory value left over from a change that never committed (e.g. an
/// <c>AIUsageRecord</c> whose in-memory status still reads Completed after Postgres reverted it to
/// Reserved).
/// </summary>
public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly CommitAheadDbContext _dbContext;

    public EfUnitOfWork(CommitAheadDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            _dbContext.ChangeTracker.Clear();
            throw;
        }
    }
}
