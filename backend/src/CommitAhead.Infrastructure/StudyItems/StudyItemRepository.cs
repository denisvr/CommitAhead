using CommitAhead.Application.StudyItems;
using CommitAhead.Domain.StudyItems;
using CommitAhead.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommitAhead.Infrastructure.StudyItems;

public sealed class StudyItemRepository : IStudyItemRepository
{
    private readonly CommitAheadDbContext _dbContext;

    public StudyItemRepository(CommitAheadDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<StudyItem?> GetByIdAsync(Guid ownerUserId, Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.StudyItems
            .Include(item => item.Reviews)
            .SingleOrDefaultAsync(item => item.OwnerUserId == ownerUserId && item.Id == id, cancellationToken);
    }

    public async Task AddAsync(StudyItem item, CancellationToken cancellationToken)
    {
        _dbContext.StudyItems.Add(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(StudyItem item, CancellationToken cancellationToken)
    {
        _dbContext.StudyItems.Remove(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
