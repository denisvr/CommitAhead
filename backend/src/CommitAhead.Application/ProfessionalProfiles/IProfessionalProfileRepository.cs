using CommitAhead.Domain.ProfessionalProfiles;

namespace CommitAhead.Application.ProfessionalProfiles;

public interface IProfessionalProfileRepository
{
    /// <summary>A singleton per owner (model.md) — there is no GetById, lookups are always by ownerUserId. Returns null before the user's first save.</summary>
    Task<ProfessionalProfile?> GetByOwnerUserIdAsync(Guid ownerUserId, CancellationToken cancellationToken);

    Task AddAsync(ProfessionalProfile profile, CancellationToken cancellationToken);

    /// <summary>Persists mutations made through ProfessionalProfile's own methods on an already-tracked entity.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
