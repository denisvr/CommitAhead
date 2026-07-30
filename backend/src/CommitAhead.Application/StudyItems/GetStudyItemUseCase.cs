using CommitAhead.Application.Identity;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Application.StudyItems;

public sealed class GetStudyItemUseCase
{
    private readonly IStudyItemRepository _repository;
    private readonly IScoringConfigRepository _scoringConfigRepository;
    private readonly IEvidenceLinkQuery _evidenceLinkQuery;
    private readonly ICurrentUser _currentUser;

    public GetStudyItemUseCase(IStudyItemRepository repository, IScoringConfigRepository scoringConfigRepository, IEvidenceLinkQuery evidenceLinkQuery, ICurrentUser currentUser)
    {
        _repository = repository;
        _scoringConfigRepository = scoringConfigRepository;
        _evidenceLinkQuery = evidenceLinkQuery;
        _currentUser = currentUser;
    }

    public async Task<StudyItemDetailResult?> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await _repository.GetByIdAsync(_currentUser.UserId, id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        var weights = await _scoringConfigRepository.GetOverrideAsync(_currentUser.UserId, cancellationToken) ?? ScoringWeights.Default;
        var mastery = item.ComputeMastery();

        // Always 0 today since no command creates EvidenceLinks yet (Phase 4) — a real query
        // against an empty table, not a hardcoded stand-in (docs/roadmap.md Phase 1).
        var demand = await _evidenceLinkQuery.GetDemandAsync(_currentUser.UserId, id, cancellationToken);
        var breakdown = EffectiveScorePolicy.ComputeBreakdown(item.Importance, demand, mastery, weights);
        var effectiveScore = EffectiveScorePolicy.Resolve(item.Importance, demand, mastery, weights, item.PriorityOverride);

        return new StudyItemDetailResult(
            item.Id,
            item.Title,
            item.Category,
            item.Status,
            item.Importance,
            item.InitialMastery,
            item.Tags,
            item.Details,
            item.PriorityOverride?.Score,
            item.PriorityOverride?.Reason,
            mastery,
            demand,
            effectiveScore,
            breakdown,
            item.Reviews.Select(review => new StudyReviewResult(review.Id, review.ReviewedAtUtc, review.ConfidenceRating, review.NotesMarkdown)).ToList(),
            item.CreatedAtUtc,
            item.UpdatedAtUtc);
    }
}

public sealed record StudyItemDetailResult(
    Guid Id,
    string Title,
    StudyItemCategory Category,
    StudyItemStatus Status,
    int Importance,
    int InitialMastery,
    IReadOnlyList<string> Tags,
    StudyItemDetails Details,
    int? PriorityOverrideScore,
    string? PriorityOverrideReason,
    decimal Mastery,
    decimal Demand,
    int EffectiveScore,
    ScoreBreakdown ScoreBreakdown,
    IReadOnlyList<StudyReviewResult> Reviews,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record StudyReviewResult(Guid Id, DateTime ReviewedAtUtc, int ConfidenceRating, string? NotesMarkdown);
