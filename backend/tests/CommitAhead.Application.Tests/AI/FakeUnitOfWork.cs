using CommitAhead.Application.Persistence;

namespace CommitAhead.Application.Tests.AI;

/// <summary>
/// Invokes the callback directly — the in-memory fakes have no real rollback semantics, so
/// atomic-commit-or-rollback itself is proven at the Infrastructure level (EfUnitOfWorkTests)
/// instead. This lets use-case-level tests exercise the transaction *call sites* without needing a
/// real transaction.
/// </summary>
public sealed class FakeUnitOfWork : IUnitOfWork
{
    public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken) =>
        operation(cancellationToken);
}
