using CommitAhead.Domain.CVPresentations;

namespace CommitAhead.Application.CVPresentations;

public interface ICVPresentationRepository
{
    /// <summary>Scoped to ownerUserId — never returns another user's CVPresentation (ADR-0015).</summary>
    Task<CVPresentation?> GetByIdAsync(Guid ownerUserId, Guid id, CancellationToken cancellationToken);

    /// <summary>Every CVPresentation owned by ownerUserId — used to find and clean up dangling selections when a canonical profile entry is removed (invariant 25).</summary>
    Task<IReadOnlyList<CVPresentation>> GetAllAsync(Guid ownerUserId, CancellationToken cancellationToken);

    Task AddAsync(CVPresentation presentation, CancellationToken cancellationToken);

    Task DeleteAsync(CVPresentation presentation, CancellationToken cancellationToken);

    /// <summary>Persists mutations made through CVPresentation's own methods on an already-tracked entity.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
