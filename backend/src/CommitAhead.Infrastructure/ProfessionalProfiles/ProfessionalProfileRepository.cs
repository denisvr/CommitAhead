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
        return _dbContext.ProfessionalProfiles
            .Include(profile => profile.Experience)
            .Include(profile => profile.Education)
            .Include(profile => profile.Skills)
            .Include(profile => profile.Languages)
            .Include(profile => profile.Certifications)
            .Include(profile => profile.Projects)
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
