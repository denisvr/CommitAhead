using CommitAhead.Domain.EvidenceLinks;

namespace CommitAhead.Application.EvidenceLinks;

/// <summary>
/// The mutable EvidenceLink port — creation only (no update; deletion is a separate future
/// DeleteEvidenceLinkUseCase). The first real creation path is ApplyAnalysisDraftUseCase, applying
/// an accepted LinkProposal.
/// </summary>
public interface IEvidenceLinkRepository
{
    /// <summary>The model.md invariant 8 check ("unique by (sourceType, sourceId, targetStudyItemId)"), scoped by owner — used to reject a duplicate before ever attempting the insert.</summary>
    Task<bool> ExistsAsync(Guid ownerUserId, EvidenceSourceType sourceType, Guid sourceId, Guid targetStudyItemId, CancellationToken cancellationToken);

    /// <summary>Throws EvidenceLinkConflictException if the database's own unique index rejects a concurrent duplicate that the caller's own ExistsAsync check missed.</summary>
    Task AddAsync(EvidenceLink link, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
