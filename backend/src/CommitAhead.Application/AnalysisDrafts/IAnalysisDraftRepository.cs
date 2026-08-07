using CommitAhead.Domain.AnalysisDrafts;
using CommitAhead.Domain.EvidenceLinks;

namespace CommitAhead.Application.AnalysisDrafts;

public interface IAnalysisDraftRepository
{
    /// <summary>Scoped to ownerUserId — never returns another user's AnalysisDraft (ADR-0015).</summary>
    Task<AnalysisDraft?> GetByIdAsync(Guid ownerUserId, Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// The "at most one Pending AnalysisDraft per source" guard (model.md) — an analyzing use case
    /// calls this before creating a new draft. The database's own partial unique index
    /// (Infrastructure) is the real, race-safe enforcement; this is the use-case-level check that
    /// produces a clean rejection instead of a raw constraint-violation exception in the common case.
    /// </summary>
    Task<AnalysisDraft?> GetPendingBySourceAsync(Guid ownerUserId, EvidenceSourceType sourceType, Guid sourceId, CancellationToken cancellationToken);

    Task AddAsync(AnalysisDraft draft, CancellationToken cancellationToken);

    /// <summary>Persists mutations made through AnalysisDraft's own methods (and its proposals') on an already-tracked entity.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
