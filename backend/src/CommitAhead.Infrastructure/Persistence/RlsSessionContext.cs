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

    public async Task RunInOwnerScopeAsync(Guid ownerUserId, Func<Task> action, CancellationToken cancellationToken)
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
            await action();
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}
