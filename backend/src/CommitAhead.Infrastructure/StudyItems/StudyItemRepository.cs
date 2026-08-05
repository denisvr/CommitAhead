using CommitAhead.Application.StudyItems;
using CommitAhead.Domain.StudyItems;
using CommitAhead.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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

    public async Task<IReadOnlyList<StudyItem>> GetAllAsync(Guid ownerUserId, CancellationToken cancellationToken)
    {
        return await _dbContext.StudyItems
            .Where(item => item.OwnerUserId == ownerUserId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(StudyItem item, CancellationToken cancellationToken)
    {
        _dbContext.StudyItems.Add(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(StudyItem item, CancellationToken cancellationToken)
    {
        _dbContext.StudyItems.Remove(item);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            // The one expected cause: the study_reviews/evidence_links Restrict FK rejected the
            // delete because a row was inserted concurrently after DeleteStudyItemUseCase's own
            // guard passed. Translate to "not deleted" here rather than letting an EF-specific
            // exception type leak into Application (which must not depend on EF Core). Any OTHER
            // DbUpdateException — a genuinely unexpected DB failure — is not this scenario and
            // must propagate normally rather than being silently swallowed into "not deleted".
            return false;
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
