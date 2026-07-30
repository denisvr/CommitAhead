using CommitAhead.Application.StudyItems;
using CommitAhead.Domain.EvidenceLinks;
using CommitAhead.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommitAhead.Infrastructure.EvidenceLinks;

public sealed class EvidenceLinkQuery : IEvidenceLinkQuery
{
    private readonly CommitAheadDbContext _dbContext;

    public EvidenceLinkQuery(CommitAheadDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<decimal> GetDemandAsync(Guid ownerUserId, Guid studyItemId, CancellationToken cancellationToken)
    {
        var links = await _dbContext.EvidenceLinks
            .Where(link => link.OwnerUserId == ownerUserId && link.TargetStudyItemId == studyItemId)
            .ToListAsync(cancellationToken);

        return DemandPolicy.Compute(links);
    }

    public Task<bool> AnyTargetingStudyItemAsync(Guid ownerUserId, Guid studyItemId, CancellationToken cancellationToken)
    {
        return _dbContext.EvidenceLinks.AnyAsync(link => link.OwnerUserId == ownerUserId && link.TargetStudyItemId == studyItemId, cancellationToken);
    }
}
