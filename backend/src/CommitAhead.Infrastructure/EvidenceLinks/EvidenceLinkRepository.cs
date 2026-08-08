using CommitAhead.Application.EvidenceLinks;
using CommitAhead.Domain.EvidenceLinks;
using CommitAhead.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CommitAhead.Infrastructure.EvidenceLinks;

public sealed class EvidenceLinkRepository : IEvidenceLinkRepository
{
    // The exact name EF generated for EvidenceLinkConfiguration's unique index (migration
    // 20260730110652_AddEvidenceLinks) — only this constraint's violation is treated as a
    // conflict; any other database failure propagates unchanged.
    private const string UniqueTargetConstraintName = "IX_evidence_links_source_type_source_id_target_study_item_id";

    private readonly CommitAheadDbContext _dbContext;

    public EvidenceLinkRepository(CommitAheadDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsAsync(Guid ownerUserId, EvidenceSourceType sourceType, Guid sourceId, Guid targetStudyItemId, CancellationToken cancellationToken)
    {
        return _dbContext.EvidenceLinks.AnyAsync(
            link => link.OwnerUserId == ownerUserId && link.SourceType == sourceType && link.SourceId == sourceId && link.TargetStudyItemId == targetStudyItemId,
            cancellationToken);
    }

    public Task<EvidenceLink?> GetByIdAsync(Guid ownerUserId, Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.EvidenceLinks.SingleOrDefaultAsync(link => link.OwnerUserId == ownerUserId && link.Id == id, cancellationToken);
    }

    public async Task AddAsync(EvidenceLink link, CancellationToken cancellationToken)
    {
        _dbContext.EvidenceLinks.Add(link);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { ConstraintName: UniqueTargetConstraintName })
        {
            throw new EvidenceLinkConflictException();
        }
    }

    public Task DeleteAsync(EvidenceLink link, CancellationToken cancellationToken)
    {
        _dbContext.EvidenceLinks.Remove(link);
        return Task.CompletedTask;
    }

    public Task DeleteAllForSourceAsync(Guid ownerUserId, EvidenceSourceType sourceType, Guid sourceId, CancellationToken cancellationToken)
    {
        return _dbContext.EvidenceLinks
            .Where(link => link.OwnerUserId == ownerUserId && link.SourceType == sourceType && link.SourceId == sourceId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
