using CommitAhead.Application.StudyItems;
using CommitAhead.Domain.EvidenceLinks;
using CommitAhead.Domain.StudyItems;
using CommitAhead.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommitAhead.Infrastructure.StudyItems;

/// <summary>
/// Loads the owner's Active StudyItems (with reviews) and ranks them in memory using the same
/// StudyItem.ComputeMastery()/EffectiveScorePolicy the domain and detail view use — one formula,
/// not a SQL re-implementation that could drift from it. Appropriate at this app's scale
/// (invite-only, so each owner's own item count stays small regardless of how many owners exist —
/// the architecture is multi-user-ready per ADR-0015, this just isn't a cross-owner query); ADR-0003
/// already defers any denormalisation until a real performance measurement calls for it.
/// </summary>
public sealed class RankedStudyQueueQuery : IRankedStudyQueueQuery
{
    private readonly CommitAheadDbContext _dbContext;

    public RankedStudyQueueQuery(CommitAheadDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<RankedStudyItem>> ExecuteAsync(Guid ownerUserId, ScoringWeights weights, CancellationToken cancellationToken)
    {
        var items = await _dbContext.StudyItems
            .Include(item => item.Reviews)
            .Where(item => item.OwnerUserId == ownerUserId && item.Status == StudyItemStatus.Active)
            .ToListAsync(cancellationToken);

        // One query for every item's links rather than one per item — always 0 links today since
        // no command creates EvidenceLinks yet (Phase 4), but the join is real (docs/roadmap.md
        // Phase 1), not a hardcoded stand-in.
        var demandByStudyItemId = (await _dbContext.EvidenceLinks
                .Where(link => link.OwnerUserId == ownerUserId)
                .ToListAsync(cancellationToken))
            .GroupBy(link => link.TargetStudyItemId)
            .ToDictionary(group => group.Key, group => DemandPolicy.Compute(group));

        return items
            .Select(item => ToRankedItem(item, weights, demandByStudyItemId.GetValueOrDefault(item.Id)))
            .OrderByDescending(ranked => ranked.EffectiveScore)
            .ThenBy(ranked => ranked.CreatedAtUtc)
            .ThenBy(ranked => ranked.Id)
            .ToList();
    }

    private static RankedStudyItem ToRankedItem(StudyItem item, ScoringWeights weights, decimal demand)
    {
        var mastery = item.ComputeMastery();
        var effectiveScore = EffectiveScorePolicy.Resolve(item.Importance, demand, mastery, weights, item.PriorityOverride);
        var lastReviewedAtUtc = item.Reviews.Count == 0 ? (DateTime?)null : item.Reviews.Max(review => review.ReviewedAtUtc);

        return new RankedStudyItem(
            item.Id,
            item.Title,
            item.Category,
            item.Importance,
            mastery,
            demand,
            effectiveScore,
            item.PriorityOverride?.Score,
            item.PriorityOverride?.Reason,
            lastReviewedAtUtc,
            item.CreatedAtUtc);
    }
}
