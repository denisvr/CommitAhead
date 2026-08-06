using CommitAhead.Application.CVPresentations;
using CommitAhead.Domain.CVPresentations;
using CommitAhead.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommitAhead.Infrastructure.CVPresentations;

public sealed class CVPresentationRepository : ICVPresentationRepository
{
    private readonly CommitAheadDbContext _dbContext;

    public CVPresentationRepository(CommitAheadDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<CVPresentation?> GetByIdAsync(Guid ownerUserId, Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.CVPresentations
            .SingleOrDefaultAsync(presentation => presentation.OwnerUserId == ownerUserId && presentation.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<CVPresentation>> GetAllAsync(Guid ownerUserId, CancellationToken cancellationToken)
    {
        return await _dbContext.CVPresentations
            .Where(presentation => presentation.OwnerUserId == ownerUserId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(CVPresentation presentation, CancellationToken cancellationToken)
    {
        _dbContext.CVPresentations.Add(presentation);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task DeleteAsync(CVPresentation presentation, CancellationToken cancellationToken)
    {
        _dbContext.CVPresentations.Remove(presentation);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
