using CommitAhead.Application.AnalysisDrafts;
using CommitAhead.Domain.AnalysisDrafts;
using CommitAhead.Domain.EvidenceLinks;
using CommitAhead.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommitAhead.Infrastructure.AnalysisDrafts;

public sealed class AnalysisDraftRepository : IAnalysisDraftRepository
{
    private readonly CommitAheadDbContext _dbContext;

    public AnalysisDraftRepository(CommitAheadDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<AnalysisDraft?> GetByIdAsync(Guid ownerUserId, Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.AnalysisDrafts
            .Include(draft => draft.SuggestionProposals)
            .Include(draft => draft.LinkProposals)
            .Include(draft => draft.StudyItemProposals)
            .SingleOrDefaultAsync(draft => draft.OwnerUserId == ownerUserId && draft.Id == id, cancellationToken);
    }

    public Task<AnalysisDraft?> GetPendingBySourceAsync(Guid ownerUserId, EvidenceSourceType sourceType, Guid sourceId, CancellationToken cancellationToken)
    {
        return _dbContext.AnalysisDrafts
            .Include(draft => draft.SuggestionProposals)
            .Include(draft => draft.LinkProposals)
            .Include(draft => draft.StudyItemProposals)
            .SingleOrDefaultAsync(
                draft => draft.OwnerUserId == ownerUserId
                    && draft.SourceType == sourceType
                    && draft.SourceId == sourceId
                    && draft.Status == AnalysisDraftStatus.Pending,
                cancellationToken);
    }

    public async Task AddAsync(AnalysisDraft draft, CancellationToken cancellationToken)
    {
        _dbContext.AnalysisDrafts.Add(draft);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
