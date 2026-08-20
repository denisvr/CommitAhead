using CommitAhead.Application.ProfessionalProfiles;
using CommitAhead.Domain.ProfessionalProfiles;
using CommitAhead.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommitAhead.Infrastructure.ProfessionalProfiles;

public sealed class ProfessionalProfileRepository : IProfessionalProfileRepository
{
    private readonly CommitAheadDbContext _dbContext;

    public ProfessionalProfileRepository(CommitAheadDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ProfessionalProfile?> GetByOwnerUserIdAsync(Guid ownerUserId, CancellationToken cancellationToken)
    {
        // Experience/Education/Certifications/Projects order by Position — the user-controlled
        // order stamped on every Replace* (ProfessionalProfile.AssignPositions). Without this,
        // Postgres has no obligation to return child rows in any particular order. Skills/
        // Languages/ProfileLinks have no such ordering concept, so they stay unordered.
        return _dbContext.ProfessionalProfiles
            .Include(profile => profile.Experience.OrderBy(entry => entry.Position))
            .Include(profile => profile.Education.OrderBy(entry => entry.Position))
            .Include(profile => profile.Skills)
            .Include(profile => profile.Languages)
            .Include(profile => profile.Certifications.OrderBy(entry => entry.Position))
            .Include(profile => profile.Projects.OrderBy(entry => entry.Position))
            .Include(profile => profile.ProfileLinks)
            .SingleOrDefaultAsync(profile => profile.OwnerUserId == ownerUserId, cancellationToken);
    }

    public async Task AddAsync(ProfessionalProfile profile, CancellationToken cancellationToken)
    {
        _dbContext.ProfessionalProfiles.Add(profile);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
