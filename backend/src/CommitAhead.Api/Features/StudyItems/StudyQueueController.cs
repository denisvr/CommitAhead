using CommitAhead.Api.Security;
using CommitAhead.Application.StudyItems;
using CommitAhead.Domain.StudyItems;
using Microsoft.AspNetCore.Mvc;

namespace CommitAhead.Api.Features.StudyItems;

[ApiController]
[Route("api/study-queue")]
[UsesOwnerScopedData]
public sealed class StudyQueueController : ControllerBase
{
    private readonly GetRankedStudyQueueUseCase _useCase;

    public StudyQueueController(GetRankedStudyQueueUseCase useCase)
    {
        _useCase = useCase;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RankedStudyItemResponse>>> Get(CancellationToken cancellationToken)
    {
        var items = await _useCase.ExecuteAsync(cancellationToken);
        return Ok(items.Select(RankedStudyItemResponse.FromResult).ToList());
    }
}

public sealed record RankedStudyItemResponse(
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
    DateTime CreatedAtUtc)
{
    public static RankedStudyItemResponse FromResult(RankedStudyItem item) => new(
        item.Id,
        item.Title,
        item.Category,
        item.Importance,
        item.Mastery,
        item.Demand,
        item.EffectiveScore,
        item.PriorityOverrideScore,
        item.PriorityOverrideReason,
        item.LastReviewedAtUtc,
        item.CreatedAtUtc);
}
