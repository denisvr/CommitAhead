using CommitAhead.Api.Security;
using CommitAhead.Application.JobAnalyses;
using CommitAhead.Domain.JobAnalyses;
using Microsoft.AspNetCore.Mvc;

namespace CommitAhead.Api.Features.JobAnalyses;

[ApiController]
[Route("api/job-analyses")]
[UsesOwnerScopedData]
public sealed class JobAnalysesController : ControllerBase
{
    private readonly GetJobAnalysisUseCase _getUseCase;
    private readonly GetJobAnalysesUseCase _getAllUseCase;
    private readonly CreateJobAnalysisUseCase _createUseCase;
    private readonly UpdateJobAnalysisUseCase _updateUseCase;
    private readonly DeleteJobAnalysisUseCase _deleteUseCase;

    public JobAnalysesController(
        GetJobAnalysisUseCase getUseCase,
        GetJobAnalysesUseCase getAllUseCase,
        CreateJobAnalysisUseCase createUseCase,
        UpdateJobAnalysisUseCase updateUseCase,
        DeleteJobAnalysisUseCase deleteUseCase)
    {
        _getUseCase = getUseCase;
        _getAllUseCase = getAllUseCase;
        _createUseCase = createUseCase;
        _updateUseCase = updateUseCase;
        _deleteUseCase = deleteUseCase;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<JobAnalysisResponse>>> Get(CancellationToken cancellationToken)
    {
        var results = await _getAllUseCase.ExecuteAsync(cancellationToken);
        return Ok(results.Select(JobAnalysisResponse.FromResult).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobAnalysisResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getUseCase.ExecuteAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(JobAnalysisResponse.FromResult(result));
    }

    [HttpPost]
    public async Task<ActionResult<JobAnalysisCreatedResponse>> Post([FromBody] CreateJobAnalysisRequest request, CancellationToken cancellationToken)
    {
        var id = await request.CreateAsync(_createUseCase, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, new JobAnalysisCreatedResponse(id));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Put(Guid id, [FromBody] UpdateJobAnalysisRequest request, CancellationToken cancellationToken)
    {
        var result = await request.UpdateAsync(_updateUseCase, id, cancellationToken);
        return result == JobAnalysisMutationResult.NotFound ? NotFound() : NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _deleteUseCase.ExecuteAsync(id, cancellationToken);
        return result == JobAnalysisMutationResult.NotFound ? NotFound() : NoContent();
    }
}

public sealed record JobAnalysisCreatedResponse(Guid Id);

/// <summary>
/// Only pasted text is acceptable here. An UploadedFile's StorageObjectKey/ExtractedText must
/// never come from a raw client request field (see CreateJobAnalysisUseCase's own trust-boundary
/// doc-comment) — this slice has no upload endpoint at all, so there is no safe way to accept an
/// UploadedFile JobSource yet. That lands together with the upload flow itself, not here.
/// </summary>
public sealed record CreateJobAnalysisRequest(string Title, string JobPostingText, string? NotesMarkdown)
{
    public Task<Guid> CreateAsync(CreateJobAnalysisUseCase useCase, CancellationToken cancellationToken)
        => useCase.ExecuteAsync(Title, new PastedText(JobPostingText), NotesMarkdown, cancellationToken);
}

/// <summary>JobSource is immutable after creation (JobAnalysis.cs) — Update never touches it, only Title/NotesMarkdown.</summary>
public sealed record UpdateJobAnalysisRequest(string Title, string? NotesMarkdown)
{
    public Task<JobAnalysisMutationResult> UpdateAsync(UpdateJobAnalysisUseCase useCase, Guid id, CancellationToken cancellationToken)
        => useCase.ExecuteAsync(id, Title, NotesMarkdown, cancellationToken);
}

public sealed record JobRequirementResponse(Guid Id, string Text, JobRequirementKind Kind, JobRequirementPriority Priority, string SourceExcerpt)
{
    public static JobRequirementResponse FromDomain(JobRequirement requirement) => new(
        requirement.Id, requirement.Text, requirement.Kind, requirement.Priority, requirement.SourceExcerpt);
}

public sealed record JobGapResponse(Guid Id, Guid RequirementId, JobGapMatchLevel MatchLevel, JobGapSeverity Severity, string Rationale)
{
    public static JobGapResponse FromDomain(JobGap gap) => new(gap.Id, gap.RequirementId, gap.MatchLevel, gap.Severity, gap.Rationale);
}

public sealed record JobAnalysisResponse(
    Guid Id,
    string Title,
    JobSourceResponse JobSource,
    IReadOnlyList<JobRequirementResponse> Requirements,
    IReadOnlyList<JobGapResponse> Gaps,
    string? NotesMarkdown,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc)
{
    public static JobAnalysisResponse FromResult(JobAnalysisResult result) => new(
        result.Id,
        result.Title,
        JobSourceResponse.FromDomain(result.JobSource),
        result.Requirements.Select(JobRequirementResponse.FromDomain).ToList(),
        result.Gaps.Select(JobGapResponse.FromDomain).ToList(),
        result.NotesMarkdown,
        result.CreatedAtUtc,
        result.UpdatedAtUtc);
}
