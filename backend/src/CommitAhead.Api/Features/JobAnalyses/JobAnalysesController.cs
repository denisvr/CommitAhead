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
    private readonly CreateJobAnalysisFromUploadUseCase _createFromUploadUseCase;
    private readonly UpdateJobAnalysisUseCase _updateUseCase;
    private readonly DeleteJobAnalysisUseCase _deleteUseCase;

    public JobAnalysesController(
        GetJobAnalysisUseCase getUseCase,
        GetJobAnalysesUseCase getAllUseCase,
        CreateJobAnalysisUseCase createUseCase,
        CreateJobAnalysisFromUploadUseCase createFromUploadUseCase,
        UpdateJobAnalysisUseCase updateUseCase,
        DeleteJobAnalysisUseCase deleteUseCase)
    {
        _getUseCase = getUseCase;
        _getAllUseCase = getAllUseCase;
        _createUseCase = createUseCase;
        _createFromUploadUseCase = createFromUploadUseCase;
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

    /// <summary>
    /// The size limits here are a coarse HTTP-boundary pre-filter (~6 MB: the 5 MB file-content
    /// cap plus headroom for multipart overhead and the other form fields) — the exact 5 MB
    /// content cap is enforced precisely inside CreateJobAnalysisFromUploadUseCase by actually
    /// counting bytes while copying, never by trusting this request's reported length.
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<ActionResult<JobAnalysisCreatedResponse>> PostUpload([FromForm] CreateJobAnalysisFromUploadRequest request, CancellationToken cancellationToken)
    {
        await using var stream = request.File.OpenReadStream();
        var id = await _createFromUploadUseCase.ExecuteAsync(
            request.Title, stream, request.File.FileName, request.File.ContentType, request.NotesMarkdown, cancellationToken);
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
/// doc-comment) — a PDF upload goes through the separate POST .../upload endpoint below, whose
/// own use case is the only thing trusted to construct an UploadedFile.
/// </summary>
public sealed record CreateJobAnalysisRequest(string Title, string JobPostingText, string? NotesMarkdown)
{
    public Task<Guid> CreateAsync(CreateJobAnalysisUseCase useCase, CancellationToken cancellationToken)
        => useCase.ExecuteAsync(Title, new PastedText(JobPostingText), NotesMarkdown, cancellationToken);
}

public sealed class CreateJobAnalysisFromUploadRequest
{
    public string Title { get; set; } = string.Empty;

    public string? NotesMarkdown { get; set; }

    public IFormFile File { get; set; } = null!;
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
