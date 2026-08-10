using CommitAhead.Application.Persistence;

namespace CommitAhead.Application.Tests.AI;

/// <summary>
/// Invokes the callback directly — the in-memory fakes have no real transaction/RLS semantics, so
/// the real owner-scoped commit-before-provider-call behavior is proven at the Infrastructure level
/// (RlsSessionContext, AnalysisCommandOrchestratorDurabilityTests) instead. This lets use-case-level
/// tests exercise the transaction *call sites* without needing a real database.
/// </summary>
public sealed class FakeRlsSessionContext : IRlsSessionContext
{
    public Task RunInOwnerScopeAsync(Guid ownerUserId, Func<Task> action, CancellationToken cancellationToken) => action();

    public Task<T> RunInOwnerScopeAsync<T>(Guid ownerUserId, Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken) =>
        action(cancellationToken);
}
