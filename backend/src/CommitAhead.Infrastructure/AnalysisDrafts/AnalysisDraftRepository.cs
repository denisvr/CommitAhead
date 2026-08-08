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

    public async Task<AnalysisDraft?> GetByIdForUpdateAsync(Guid ownerUserId, Guid id, CancellationToken cancellationToken)
    {
        if (_dbContext.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("GetByIdForUpdateAsync must be called inside an active transaction — an unlocked read would defeat its own purpose.");
        }

        // Acquires and holds the row lock for the ambient transaction's duration; the result set
        // itself is discarded — the lock, not the data, is the point. Same DbContext/connection/
        // transaction as the Include-based load below and as whatever later calls SaveChangesAsync.
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM analysis_drafts WHERE id = {id} AND owner_user_id = {ownerUserId} FOR UPDATE",
            cancellationToken);

        return await _dbContext.AnalysisDrafts
            .Include(draft => draft.SuggestionProposals)
            .Include(draft => draft.LinkProposals)
            .Include(draft => draft.StudyItemProposals)
            .SingleOrDefaultAsync(draft => draft.OwnerUserId == ownerUserId && draft.Id == id, cancellationToken);
    }

    public async Task AddAsync(AnalysisDraft draft, CancellationToken cancellationToken)
    {
        _dbContext.AnalysisDrafts.Add(draft);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task DeleteAllForSourceAsync(Guid ownerUserId, EvidenceSourceType sourceType, Guid sourceId, CancellationToken cancellationToken)
    {
        // Proposal children cascade via AnalysisDraftConfiguration's own ON DELETE CASCADE foreign
        // keys — no status filter, per ADR-0011 ("including a Pending draft if one exists").
        return _dbContext.AnalysisDrafts
            .Where(draft => draft.OwnerUserId == ownerUserId && draft.SourceType == sourceType && draft.SourceId == sourceId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
