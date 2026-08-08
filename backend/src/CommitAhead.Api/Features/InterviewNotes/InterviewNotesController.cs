using CommitAhead.Api.Features.AnalysisDrafts;
using CommitAhead.Api.Security;
using CommitAhead.Application.AI;
using CommitAhead.Application.InterviewNotes;
using CommitAhead.Domain.InterviewNotes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CommitAhead.Api.Features.InterviewNotes;

[ApiController]
[Route("api/interview-notes")]
[UsesOwnerScopedData]
public sealed class InterviewNotesController : ControllerBase
{
    private readonly GetInterviewNoteUseCase _getUseCase;
    private readonly GetInterviewNotesUseCase _getAllUseCase;
    private readonly CreateInterviewNoteUseCase _createUseCase;
    private readonly UpdateInterviewNoteUseCase _updateUseCase;
    private readonly DeleteInterviewNoteUseCase _deleteUseCase;
    private readonly AnalyzeInterviewNoteUseCase _analyzeUseCase;

    public InterviewNotesController(
        GetInterviewNoteUseCase getUseCase,
        GetInterviewNotesUseCase getAllUseCase,
        CreateInterviewNoteUseCase createUseCase,
        UpdateInterviewNoteUseCase updateUseCase,
        DeleteInterviewNoteUseCase deleteUseCase,
        AnalyzeInterviewNoteUseCase analyzeUseCase)
    {
        _getUseCase = getUseCase;
        _getAllUseCase = getAllUseCase;
        _createUseCase = createUseCase;
        _updateUseCase = updateUseCase;
        _deleteUseCase = deleteUseCase;
        _analyzeUseCase = analyzeUseCase;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InterviewNoteResponse>>> Get(CancellationToken cancellationToken)
    {
        var results = await _getAllUseCase.ExecuteAsync(cancellationToken);
        return Ok(results.Select(InterviewNoteResponse.FromResult).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InterviewNoteResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getUseCase.ExecuteAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(InterviewNoteResponse.FromResult(result));
    }

    [HttpPost]
    public async Task<ActionResult<InterviewNoteCreatedResponse>> Post([FromBody] CreateInterviewNoteRequest request, CancellationToken cancellationToken)
    {
        // A jobAnalysisId that doesn't resolve to the current user's own JobAnalysis throws
        // DomainValidationException (invariant 29); ValidationExceptionFilter maps that to
        // 422 for every controller in this API, so no local try/catch is needed here.
        var id = await request.CreateAsync(_createUseCase, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, new InterviewNoteCreatedResponse(id));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Put(Guid id, [FromBody] UpdateInterviewNoteRequest request, CancellationToken cancellationToken)
    {
        var result = await request.UpdateAsync(_updateUseCase, id, cancellationToken);
        return result == InterviewNoteMutationResult.NotFound ? NotFound() : NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _deleteUseCase.ExecuteAsync(id, cancellationToken);
        return result == InterviewNoteMutationResult.NotFound ? NotFound() : NoContent();
    }

    [HttpPost("{id:guid}/analyze")]
    [EnableRateLimiting("ai-analysis")]
    public async Task<ActionResult<AnalyzeCommandResponse>> Analyze(Guid id, [FromBody] AnalyzeCommandRequest request, CancellationToken cancellationToken)
    {
        var result = await _analyzeUseCase.ExecuteAsync(id, request.IdempotencyKey, cancellationToken);
        var response = new AnalyzeCommandResponse(result.Outcome, result.AnalysisDraftId);

        return result.Outcome switch
        {
            AnalyzeCommandOutcome.SourceNotFound => NotFound(),
            AnalyzeCommandOutcome.Created => StatusCode(StatusCodes.Status201Created, response),
            AnalyzeCommandOutcome.AlreadyCompleted => Ok(response),
            AnalyzeCommandOutcome.DailyBudgetExceeded or AnalyzeCommandOutcome.MonthlyBudgetExceeded => AiOutcomeResponses.BudgetExceeded(result.Outcome, Response),
            _ => AiOutcomeResponses.Conflict(result.Outcome.ToString()),
        };
    }
}

public sealed record InterviewNoteCreatedResponse(Guid Id);

public sealed record CreateInterviewNoteRequest(
    string Company,
    string Role,
    InterviewRound InterviewRound,
    int SequenceNumber,
    string? OtherLabel,
    DateOnly Date,
    IReadOnlyList<string> Questions,
    IReadOnlyList<string> Gaps,
    IReadOnlyList<string> Lessons,
    Guid? JobAnalysisId)
{
    public Task<Guid> CreateAsync(CreateInterviewNoteUseCase useCase, CancellationToken cancellationToken)
        => useCase.ExecuteAsync(Company, Role, InterviewRound, SequenceNumber, OtherLabel, Date, Questions, Gaps, Lessons, JobAnalysisId, cancellationToken);
}

public sealed record UpdateInterviewNoteRequest(
    string Company,
    string Role,
    InterviewRound InterviewRound,
    int SequenceNumber,
    string? OtherLabel,
    DateOnly Date,
    IReadOnlyList<string> Questions,
    IReadOnlyList<string> Gaps,
    IReadOnlyList<string> Lessons,
    Guid? JobAnalysisId)
{
    public Task<InterviewNoteMutationResult> UpdateAsync(UpdateInterviewNoteUseCase useCase, Guid id, CancellationToken cancellationToken)
        => useCase.ExecuteAsync(id, Company, Role, InterviewRound, SequenceNumber, OtherLabel, Date, Questions, Gaps, Lessons, JobAnalysisId, cancellationToken);
}

public sealed record InterviewNoteResponse(
    Guid Id,
    string Company,
    string Role,
    InterviewRound InterviewRound,
    int SequenceNumber,
    string? OtherLabel,
    DateOnly Date,
    IReadOnlyList<string> Questions,
    IReadOnlyList<string> Gaps,
    IReadOnlyList<string> Lessons,
    Guid? JobAnalysisId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc)
{
    public static InterviewNoteResponse FromResult(InterviewNoteResult result) => new(
        result.Id,
        result.Company,
        result.Role,
        result.InterviewRound,
        result.SequenceNumber,
        result.OtherLabel,
        result.Date,
        result.Questions,
        result.Gaps,
        result.Lessons,
        result.JobAnalysisId,
        result.CreatedAtUtc,
        result.UpdatedAtUtc);
}
