using CommitAhead.Domain.AIUsage;

namespace CommitAhead.Application.AIUsage;

public interface IAIUsageRecordRepository
{
    /// <summary>Scoped to ownerUserId — never returns another user's AIUsageRecord (ADR-0015). The idempotency check (ADR-0014) reads this before ever calling IAIProvider.</summary>
    Task<AIUsageRecord?> GetByIdempotencyKeyAsync(Guid ownerUserId, string idempotencyKey, CancellationToken cancellationToken);

    /// <summary>
    /// This owner's current Reserved record, if any — at most one can exist at a time (the
    /// per-owner partial-unique index). Used by the reservation step to reconcile a stale
    /// (crashed/hung) reservation before inserting a new one.
    /// </summary>
    Task<AIUsageRecord?> GetActiveReservationByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken);

    /// <summary>Throws AIUsageReservationConflictException (Application-level, no assumed reason) if a concurrent insert violates either the per-owner idempotency-key or Reserved-lock unique index.</summary>
    Task AddAsync(AIUsageRecord record, CancellationToken cancellationToken);

    /// <summary>Persists mutations made through AIUsageRecord's own methods (Complete/Fail) on an already-tracked entity.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Completed actual cost plus active Reserved cost within [windowStartUtc, windowEndUtc) —
    /// ADR-0014's budget check. The caller passes in whatever daily/monthly boundaries it needs
    /// checked; this repository has no notion of "today" or "this month" itself.
    /// </summary>
    Task<decimal> GetSpentCostAsync(Guid ownerUserId, DateTime windowStartUtc, DateTime windowEndUtc, CancellationToken cancellationToken);
}
