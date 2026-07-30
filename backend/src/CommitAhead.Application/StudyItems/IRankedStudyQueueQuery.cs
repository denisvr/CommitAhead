using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Application.StudyItems;

public interface IRankedStudyQueueQuery
{
    /// <summary>
    /// Active StudyItems for ownerUserId, ordered by EffectiveScore DESC, CreatedAt ASC, Id ASC
    /// (docs/architecture/persistence.md). Mastery, Demand and EffectiveScore are computed here,
    /// not loaded from a stored column (ADR-0003).
    /// </summary>
    Task<IReadOnlyList<RankedStudyItem>> ExecuteAsync(Guid ownerUserId, ScoringWeights weights, CancellationToken cancellationToken);
}

public sealed record RankedStudyItem(
    Guid Id,
    string Title,
    StudyItemCategory Category,
    int Importance,
    decimal Mastery,
    decimal Demand,
    int EffectiveScore,
    int? PriorityOverrideScore,
    string? PriorityOverrideReason,
    DateTime? LastReviewedAtUtc,
    DateTime CreatedAtUtc);
