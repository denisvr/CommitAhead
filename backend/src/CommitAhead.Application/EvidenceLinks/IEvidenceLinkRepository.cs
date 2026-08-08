using CommitAhead.Domain.EvidenceLinks;

namespace CommitAhead.Application.EvidenceLinks;

/// <summary>
/// The mutable EvidenceLink port. Creation happens via ApplyAnalysisDraftUseCase applying an
/// accepted LinkProposal; deletion happens standalone (DeleteEvidenceLinkUseCase) or in bulk as
/// part of ADR-0011's source-deletion cleanup (DeleteAllForSourceAsync).
/// </summary>
public interface IEvidenceLinkRepository
{
    /// <summary>The model.md invariant 8 check ("unique by (sourceType, sourceId, targetStudyItemId)"), scoped by owner — used to reject a duplicate before ever attempting the insert.</summary>
    Task<bool> ExistsAsync(Guid ownerUserId, EvidenceSourceType sourceType, Guid sourceId, Guid targetStudyItemId, CancellationToken cancellationToken);

    /// <summary>Scoped to ownerUserId — never returns another user's EvidenceLink (ADR-0015).</summary>
    Task<EvidenceLink?> GetByIdAsync(Guid ownerUserId, Guid id, CancellationToken cancellationToken);

    /// <summary>Throws EvidenceLinkConflictException if the database's own unique index rejects a concurrent duplicate that the caller's own ExistsAsync check missed.</summary>
    Task AddAsync(EvidenceLink link, CancellationToken cancellationToken);

    /// <summary>Marks the link for deletion; SaveChangesAsync persists it.</summary>
    Task DeleteAsync(EvidenceLink link, CancellationToken cancellationToken);

    /// <summary>
    /// ADR-0011 source-deletion cleanup — bulk-deletes every EvidenceLink for one polymorphic
    /// source, regardless of target, without loading them first.
    /// </summary>
    Task DeleteAllForSourceAsync(Guid ownerUserId, EvidenceSourceType sourceType, Guid sourceId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
