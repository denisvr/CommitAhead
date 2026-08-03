using CommitAhead.Api.Security;
using CommitAhead.Application.StudyItems;
using CommitAhead.Domain.StudyItems;
using Microsoft.AspNetCore.Mvc;

namespace CommitAhead.Api.Features.StudyItems;

[ApiController]
[Route("api/study-items")]
[UsesOwnerScopedData]
public sealed class StudyItemsController : ControllerBase
{
    private readonly CreateStudyItemUseCase _createUseCase;
    private readonly GetStudyItemsUseCase _getListUseCase;
    private readonly GetStudyItemUseCase _getUseCase;
    private readonly UpdateStudyItemUseCase _updateUseCase;
    private readonly ArchiveStudyItemUseCase _archiveUseCase;
    private readonly RestoreStudyItemUseCase _restoreUseCase;
    private readonly DeleteStudyItemUseCase _deleteUseCase;
    private readonly SubmitStudyReviewUseCase _submitReviewUseCase;
    private readonly SetPriorityOverrideUseCase _setPriorityOverrideUseCase;
    private readonly ClearPriorityOverrideUseCase _clearPriorityOverrideUseCase;

    public StudyItemsController(
        CreateStudyItemUseCase createUseCase,
        GetStudyItemsUseCase getListUseCase,
        GetStudyItemUseCase getUseCase,
        UpdateStudyItemUseCase updateUseCase,
        ArchiveStudyItemUseCase archiveUseCase,
        RestoreStudyItemUseCase restoreUseCase,
        DeleteStudyItemUseCase deleteUseCase,
        SubmitStudyReviewUseCase submitReviewUseCase,
        SetPriorityOverrideUseCase setPriorityOverrideUseCase,
        ClearPriorityOverrideUseCase clearPriorityOverrideUseCase)
    {
        _createUseCase = createUseCase;
        _getListUseCase = getListUseCase;
        _getUseCase = getUseCase;
        _updateUseCase = updateUseCase;
        _archiveUseCase = archiveUseCase;
        _restoreUseCase = restoreUseCase;
        _deleteUseCase = deleteUseCase;
        _submitReviewUseCase = submitReviewUseCase;
        _setPriorityOverrideUseCase = setPriorityOverrideUseCase;
        _clearPriorityOverrideUseCase = clearPriorityOverrideUseCase;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StudyItemSummaryResponse>>> Get([FromQuery] string? status, CancellationToken cancellationToken)
    {
        var results = await _getListUseCase.ExecuteAsync(status, cancellationToken);
        return Ok(results.Select(StudyItemSummaryResponse.FromResult).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<StudyItemCreatedResponse>> Post([FromBody] CreateStudyItemRequest request, CancellationToken cancellationToken)
    {
        // StudyItem's constructor validates title/importance/initialMastery/category-details
        // agreement and throws ArgumentException; DomainValidationExceptionFilter maps that to
        // 422 for every controller in this API, so no local try/catch is needed here.
        var id = await request.CreateAsync(_createUseCase, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, new StudyItemCreatedResponse(id));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StudyItemResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getUseCase.ExecuteAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(StudyItemResponse.FromResult(result));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Put(Guid id, [FromBody] UpdateStudyItemRequest request, CancellationToken cancellationToken)
    {
        var result = await request.UpdateAsync(_updateUseCase, id, cancellationToken);
        return result == StudyItemMutationResult.NotFound ? NotFound() : NoContent();
    }

    [HttpPost("{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        var result = await _archiveUseCase.ExecuteAsync(id, cancellationToken);
        return result == StudyItemMutationResult.NotFound ? NotFound() : NoContent();
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken cancellationToken)
    {
        var result = await _restoreUseCase.ExecuteAsync(id, cancellationToken);
        return result == StudyItemMutationResult.NotFound ? NotFound() : NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _deleteUseCase.ExecuteAsync(id, cancellationToken);
        return result switch
        {
            DeleteStudyItemResult.Success => NoContent(),
            DeleteStudyItemResult.NotFound => NotFound(),
            DeleteStudyItemResult.Blocked => Conflict(),
            _ => throw new ArgumentOutOfRangeException(nameof(result)),
        };
    }

    [HttpPost("{id:guid}/reviews")]
    public async Task<IActionResult> SubmitReview(Guid id, [FromBody] SubmitStudyReviewRequest request, CancellationToken cancellationToken)
    {
        var result = await _submitReviewUseCase.ExecuteAsync(id, request.ConfidenceRating, request.NotesMarkdown, cancellationToken);
        return result == StudyItemMutationResult.NotFound ? NotFound() : NoContent();
    }

    [HttpPut("{id:guid}/priority-override")]
    public async Task<IActionResult> SetPriorityOverride(Guid id, [FromBody] SetPriorityOverrideRequest request, CancellationToken cancellationToken)
    {
        var result = await _setPriorityOverrideUseCase.ExecuteAsync(id, request.Score, request.Reason, cancellationToken);
        return result == StudyItemMutationResult.NotFound ? NotFound() : NoContent();
    }

    [HttpDelete("{id:guid}/priority-override")]
    public async Task<IActionResult> ClearPriorityOverride(Guid id, CancellationToken cancellationToken)
    {
        var result = await _clearPriorityOverrideUseCase.ExecuteAsync(id, cancellationToken);
        return result == StudyItemMutationResult.NotFound ? NotFound() : NoContent();
    }
}

public sealed record StudyItemCreatedResponse(Guid Id);

public sealed record StudyItemSummaryResponse(
    Guid Id,
    string Title,
    StudyItemCategory Category,
    StudyItemStatus Status,
    int Importance,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc)
{
    public static StudyItemSummaryResponse FromResult(StudyItemSummary result) => new(
        result.Id, result.Title, result.Category, result.Status, result.Importance, result.CreatedAtUtc, result.UpdatedAtUtc);
}

public sealed record SubmitStudyReviewRequest(int ConfidenceRating, string? NotesMarkdown);

public sealed record SetPriorityOverrideRequest(int Score, string Reason);

/// <summary>Category is fixed at creation (ADR-0001) — Update never changes it, only Title/Importance/Tags/Details.</summary>
public sealed record CreateStudyItemRequest(
    string Title,
    StudyItemCategory Category,
    int Importance,
    int InitialMastery,
    IReadOnlyList<string> Tags,
    StudyItemDetailsDto Details)
{
    public Task<Guid> CreateAsync(CreateStudyItemUseCase useCase, CancellationToken cancellationToken)
        => useCase.ExecuteAsync(Title, Category, Importance, InitialMastery, Tags, Details.ToDomain(), cancellationToken);
}

public sealed record UpdateStudyItemRequest(string Title, int Importance, IReadOnlyList<string> Tags, StudyItemDetailsDto Details)
{
    public Task<StudyItemMutationResult> UpdateAsync(UpdateStudyItemUseCase useCase, Guid id, CancellationToken cancellationToken)
        => useCase.ExecuteAsync(id, Title, Importance, Tags, Details.ToDomain(), cancellationToken);
}

public sealed record ScoreBreakdownResponse(decimal ImportanceContribution, decimal DemandContribution, decimal MasteryGapContribution, int Total)
{
    public static ScoreBreakdownResponse FromDomain(ScoreBreakdown breakdown)
        => new(breakdown.ImportanceContribution, breakdown.DemandContribution, breakdown.MasteryGapContribution, breakdown.Total);
}

public sealed record StudyReviewResponse(Guid Id, DateTime ReviewedAtUtc, int ConfidenceRating, string? NotesMarkdown)
{
    public static StudyReviewResponse FromResult(StudyReviewResult result)
        => new(result.Id, result.ReviewedAtUtc, result.ConfidenceRating, result.NotesMarkdown);
}

public sealed record StudyItemResponse(
    Guid Id,
    string Title,
    StudyItemCategory Category,
    StudyItemStatus Status,
    int Importance,
    int InitialMastery,
    IReadOnlyList<string> Tags,
    StudyItemDetailsDto Details,
    int? PriorityOverrideScore,
    string? PriorityOverrideReason,
    decimal Mastery,
    decimal Demand,
    int EffectiveScore,
    ScoreBreakdownResponse ScoreBreakdown,
    bool CanHardDelete,
    IReadOnlyList<StudyReviewResponse> Reviews,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc)
{
    public static StudyItemResponse FromResult(StudyItemDetailResult result) => new(
        result.Id,
        result.Title,
        result.Category,
        result.Status,
        result.Importance,
        result.InitialMastery,
        result.Tags,
        StudyItemDetailsDto.FromDomain(result.Details),
        result.PriorityOverrideScore,
        result.PriorityOverrideReason,
        result.Mastery,
        result.Demand,
        result.EffectiveScore,
        ScoreBreakdownResponse.FromDomain(result.ScoreBreakdown),
        result.CanHardDelete,
        result.Reviews.Select(StudyReviewResponse.FromResult).ToList(),
        result.CreatedAtUtc,
        result.UpdatedAtUtc);
}
