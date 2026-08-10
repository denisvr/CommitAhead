using CommitAhead.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommitAhead.Infrastructure.Persistence;

public sealed class RlsSessionContext : IRlsSessionContext
{
    private readonly CommitAheadDbContext _dbContext;

    public RlsSessionContext(CommitAheadDbContext dbContext)
    {
        _dbContext = dbContext;
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
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}
