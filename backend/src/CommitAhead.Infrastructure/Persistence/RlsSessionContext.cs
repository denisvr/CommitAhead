using CommitAhead.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CommitAhead.Infrastructure.Persistence;

public sealed class RlsSessionContext : IRlsSessionContext
{
    private static readonly TimeSpan RollbackTimeout = TimeSpan.FromSeconds(5);

    private readonly CommitAheadDbContext _dbContext;
    private readonly ILogger<RlsSessionContext> _logger;

    public RlsSessionContext(CommitAheadDbContext dbContext, ILogger<RlsSessionContext> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public Task RunInOwnerScopeAsync(Guid ownerUserId, Func<Task> action, CancellationToken cancellationToken) =>
        RunInOwnerScopeAsync<object?>(
            ownerUserId,
            async ct =>
            {
                await action();
                return null;
            },
            cancellationToken);

    public async Task<T> RunInOwnerScopeAsync<T>(Guid ownerUserId, Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        // set_config(..., is_local: true) is transaction-scoped — it is unset the instant this
        // transaction commits or rolls back, so a later request reusing the same pooled physical
        // connection never sees it. Outside an explicit transaction each statement is its own
        // implicit transaction, so set_config alone (without the transaction wrapping it and the
        // real query together) would already be gone by the time the next statement ran.
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT set_config('app.current_user_id', {ownerUserId.ToString()}, true)", cancellationToken);

        try
        {
            var result = await action(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            // Rollback uses its own short independent token — never the caller's, which may
            // already be why the operation failed. ChangeTracker.Clear() always runs, even if
            // rollback itself throws — without it, an entity mutated during the failed attempt
            // (e.g. an AIUsageRecord whose in-memory status still reads Completed after Postgres
            // reverted it to Reserved) would stay tracked with its stale in-memory state, and a
            // later query on this same DbContext (failure reconciliation) could return that stale
            // instance instead of a fresh one reflecting the database's actual post-rollback state.
            // A rollback failure is logged with only its exception type, never a message/object,
            // and never replaces the original exception — the bare `throw;` below always re-raises
            // it. Mirrors EfUnitOfWork's identical rollback pattern.
            using var rollbackCts = new CancellationTokenSource(RollbackTimeout);

            try
            {
                await transaction.RollbackAsync(rollbackCts.Token);
            }
            catch (Exception rollbackException)
            {
                _logger.LogWarning(
                    "Failed to roll back an owner-scoped transaction after an operation failure. RollbackExceptionType: {RollbackExceptionType}.",
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
