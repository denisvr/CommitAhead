namespace CommitAhead.Application.Persistence;

/// <summary>
/// Runs a unit of work inside a database transaction that carries the authenticated owner's id as
/// a Postgres transaction-local setting (docs/architecture/persistence.md "Supabase RLS") — the
/// per-owner RLS policies on every Phase 1 table read it via
/// current_setting('app.current_user_id', true). Transaction-local (not session/connection-level)
/// so the value can never leak into a later request that reuses the same pooled physical
/// connection: it is cleared automatically when the transaction ends, regardless of outcome.
/// </summary>
public interface IRlsSessionContext
{
    Task RunInOwnerScopeAsync(Guid ownerUserId, Func<Task> action, CancellationToken cancellationToken);

    /// <summary>Same owner-scoped transaction as the non-generic overload, but returns a value — the shape AnalysisCommandOrchestrator's multiple, independently-committed phases need (ADR-0014).</summary>
    Task<T> RunInOwnerScopeAsync<T>(Guid ownerUserId, Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken);
}
