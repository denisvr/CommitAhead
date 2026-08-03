using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Application.StudyItems;

public interface IStudyItemRepository
{
    /// <summary>Scoped to ownerUserId — never returns another user's StudyItem (ADR-0015).</summary>
    Task<StudyItem?> GetByIdAsync(Guid ownerUserId, Guid id, CancellationToken cancellationToken);

    Task AddAsync(StudyItem item, CancellationToken cancellationToken);

    /// <summary>
    /// True if deleted; false if the database rejected it (a StudyReview or EvidenceLink was
    /// inserted concurrently after the use case's own guard passed — the database is the final
    /// line of defence for that race, per docs/domain/model.md invariant 2).
    /// </summary>
    Task<bool> DeleteAsync(StudyItem item, CancellationToken cancellationToken);

    /// <summary>Persists mutations made through StudyItem's own methods on an already-tracked entity.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
