namespace CommitAhead.Application.AI;

/// <summary>
/// Thrown by <c>IAIUsageRecordRepository.AddAsync</c> when a Postgres unique-constraint violation
/// fires on insert. Deliberately carries no reason — a concurrent insert can violate either the
/// per-owner idempotency-key index or the per-owner Reserved-lock index, and Postgres does not
/// guarantee which is reported first. The caller (AnalyzeJobAnalysisUseCase) resolves the actual
/// outcome itself by re-reading the record by (ownerUserId, idempotencyKey) rather than trusting a
/// constraint name.
/// </summary>
public sealed class AIUsageReservationConflictException : Exception
{
    public AIUsageReservationConflictException()
        : base("A concurrent AI usage reservation conflict occurred.")
    {
    }
}
