using CommitAhead.Application.Persistence;
using Microsoft.Extensions.Logging;

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
///
/// If a transaction is already active on this DbContext — e.g. RlsTransactionActionFilter's own
/// owner-scoped transaction, active for the whole duration of a [UsesOwnerScopedData] controller
/// action — this nests inside it instead of beginning a second one: Npgsql/EF Core does not
/// support two transactions on the same connection at once. In that case, this call does not
/// commit or roll back anything itself; the ambient transaction's own commit/rollback governs.
/// </summary>
public sealed class EfUnitOfWork : IUnitOfWork
{
    private static readonly TimeSpan RollbackTimeout = TimeSpan.FromSeconds(5);

    private readonly CommitAheadDbContext _dbContext;
    private readonly ILogger<EfUnitOfWork> _logger;

    public EfUnitOfWork(CommitAheadDbContext dbContext, ILogger<EfUnitOfWork> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        if (_dbContext.Database.CurrentTransaction is not null)
        {
            return operation(cancellationToken);
        }

        return ExecuteInNewTransactionAsync(operation, cancellationToken);
    }

    private async Task<T> ExecuteInNewTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
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
            // Rollback uses its own short independent token — never the caller's, which may
            // already be why the operation failed (a cancelled caller token must not also abort
            // the cleanup). ChangeTracker.Clear() always runs, even if rollback itself throws. A
            // rollback failure is logged with only its exception type, never a message/object. The
            // bare `throw;` below always re-raises the *original* exception, never a rollback-path
            // one.
            using var rollbackCts = new CancellationTokenSource(RollbackTimeout);

            try
            {
                await transaction.RollbackAsync(rollbackCts.Token);
            }
            catch (Exception rollbackException)
            {
                _logger.LogWarning(
                    "Failed to roll back a transaction after an operation failure. RollbackExceptionType: {RollbackExceptionType}.",
                    rollbackException.GetType().Name);
            }
            finally
            {
                _dbContext.ChangeTracker.Clear();
            }

            throw;
        }
    }
}
