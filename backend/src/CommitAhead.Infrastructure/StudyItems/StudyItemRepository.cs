using CommitAhead.Application.StudyItems;
using CommitAhead.Domain.StudyItems;
using CommitAhead.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CommitAhead.Infrastructure.StudyItems;

public sealed class StudyItemRepository : IStudyItemRepository
{
    // The only two FK constraints that can turn a StudyItem delete into "not deleted" rather than
    // a real error — see AddPhase1OwnerForeignKeysAndReviewRestrict, which names both explicitly
    // and sets them Restrict. Any other constraint name means something else went wrong.
    private const string StudyReviewsFkConstraintName = "FK_study_reviews_study_items_study_item_id";
    private const string EvidenceLinksFkConstraintName = "FK_evidence_links_study_items_owner_user_id_target_study_item_~";

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
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.ForeignKeyViolation,
            ConstraintName: StudyReviewsFkConstraintName or EvidenceLinksFkConstraintName,
        })
        {
            // The one expected cause: the study_reviews/evidence_links Restrict FK rejected the
            // delete because a row was inserted concurrently after DeleteStudyItemUseCase's own
            // guard passed. Translate to "not deleted" here rather than letting an EF-specific
            // exception type leak into Application (which must not depend on EF Core). Matching on
            // ConstraintName, not just the FK-violation SQL state, means an FK violation against
            // some other, unanticipated constraint is not silently treated as "not deleted" either
            // — it propagates like any other unexpected DbUpdateException.
            return false;
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
