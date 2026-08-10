using CommitAhead.Api.Features.AnalysisDrafts;
using CommitAhead.Api.Security;
using CommitAhead.Application.AI;
using CommitAhead.Application.CVPresentations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CommitAhead.Api.Features.CVPresentations;

/// <summary>Multi-row per owner (model.md) — unlike ProfessionalProfile, every route below is scoped by {id}.</summary>
[ApiController]
[Route("api/cv-presentations")]
public sealed class CVPresentationController : ControllerBase
{
    private readonly GetCVPresentationUseCase _getUseCase;
    private readonly GetCVPresentationsUseCase _getAllUseCase;
    private readonly CreateCVPresentationUseCase _createUseCase;
    private readonly UpdateCVPresentationUseCase _updateUseCase;
    private readonly DeleteCVPresentationUseCase _deleteUseCase;
    private readonly ReplaceExperienceSelectionsUseCase _replaceExperienceSelectionsUseCase;
    private readonly ReplaceEducationSelectionsUseCase _replaceEducationSelectionsUseCase;
    private readonly ReplaceSkillSelectionsUseCase _replaceSkillSelectionsUseCase;
    private readonly ReplaceLanguageSelectionsUseCase _replaceLanguageSelectionsUseCase;
    private readonly ReplaceCertificationSelectionsUseCase _replaceCertificationSelectionsUseCase;
    private readonly ReplaceProjectSelectionsUseCase _replaceProjectSelectionsUseCase;
    private readonly ReplaceProfileLinkSelectionsUseCase _replaceProfileLinkSelectionsUseCase;
    private readonly AnalyzeCVPresentationUseCase _analyzeUseCase;

    public CVPresentationController(
        GetCVPresentationUseCase getUseCase,
        GetCVPresentationsUseCase getAllUseCase,
        CreateCVPresentationUseCase createUseCase,
        UpdateCVPresentationUseCase updateUseCase,
        DeleteCVPresentationUseCase deleteUseCase,
        ReplaceExperienceSelectionsUseCase replaceExperienceSelectionsUseCase,
        ReplaceEducationSelectionsUseCase replaceEducationSelectionsUseCase,
        ReplaceSkillSelectionsUseCase replaceSkillSelectionsUseCase,
        ReplaceLanguageSelectionsUseCase replaceLanguageSelectionsUseCase,
        ReplaceCertificationSelectionsUseCase replaceCertificationSelectionsUseCase,
        ReplaceProjectSelectionsUseCase replaceProjectSelectionsUseCase,
        ReplaceProfileLinkSelectionsUseCase replaceProfileLinkSelectionsUseCase,
        AnalyzeCVPresentationUseCase analyzeUseCase)
    {
        _getUseCase = getUseCase;
        _getAllUseCase = getAllUseCase;
        _createUseCase = createUseCase;
        _updateUseCase = updateUseCase;
        _deleteUseCase = deleteUseCase;
        _replaceExperienceSelectionsUseCase = replaceExperienceSelectionsUseCase;
        _replaceEducationSelectionsUseCase = replaceEducationSelectionsUseCase;
        _replaceSkillSelectionsUseCase = replaceSkillSelectionsUseCase;
        _replaceLanguageSelectionsUseCase = replaceLanguageSelectionsUseCase;
        _replaceCertificationSelectionsUseCase = replaceCertificationSelectionsUseCase;
        _replaceProjectSelectionsUseCase = replaceProjectSelectionsUseCase;
        _replaceProfileLinkSelectionsUseCase = replaceProfileLinkSelectionsUseCase;
        _analyzeUseCase = analyzeUseCase;
    }

    [HttpGet]
    [UsesOwnerScopedData]
    public async Task<ActionResult<IReadOnlyList<CVPresentationResponse>>> Get(CancellationToken cancellationToken)
    {
        var results = await _getAllUseCase.ExecuteAsync(cancellationToken);
        return Ok(results.Select(CVPresentationResponse.FromResult).ToList());
    }

    [HttpGet("{id:guid}")]
    [UsesOwnerScopedData]
    public async Task<ActionResult<CVPresentationResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getUseCase.ExecuteAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(CVPresentationResponse.FromResult(result));
    }

    [HttpPost]
    [UsesOwnerScopedData]
    public async Task<ActionResult<CVPresentationCreatedResponse>> Post([FromBody] CreateCVPresentationRequest request, CancellationToken cancellationToken)
    {
        var id = await request.CreateAsync(_createUseCase, cancellationToken);
        return id is null ? UnprocessableEntity() : CreatedAtAction(nameof(GetById), new { id }, new CVPresentationCreatedResponse(id.Value));
    }

    [HttpPut("{id:guid}")]
    [UsesOwnerScopedData]
    public async Task<IActionResult> Put(Guid id, [FromBody] UpdateCVPresentationRequest request, CancellationToken cancellationToken)
    {
        var result = await request.UpdateAsync(_updateUseCase, id, cancellationToken);
        return result == CVPresentationMutationResult.NotFound ? NotFound() : NoContent();
    }

    [HttpDelete("{id:guid}")]
    [UsesOwnerScopedData]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _deleteUseCase.ExecuteAsync(id, cancellationToken);
        return result == CVPresentationMutationResult.NotFound ? NotFound() : NoContent();
    }

    [HttpPut("{id:guid}/experience-selections")]
    [UsesOwnerScopedData]
    public async Task<IActionResult> PutExperienceSelections(Guid id, [FromBody] IReadOnlyList<Guid> entryIds, CancellationToken cancellationToken)
    {
        var result = await _replaceExperienceSelectionsUseCase.ExecuteAsync(id, entryIds, cancellationToken);
        return result == CVPresentationMutationResult.NotFound ? NotFound() : NoContent();
    }

    [HttpPut("{id:guid}/education-selections")]
    [UsesOwnerScopedData]
    public async Task<IActionResult> PutEducationSelections(Guid id, [FromBody] IReadOnlyList<Guid> entryIds, CancellationToken cancellationToken)
    {
        var result = await _replaceEducationSelectionsUseCase.ExecuteAsync(id, entryIds, cancellationToken);
        return result == CVPresentationMutationResult.NotFound ? NotFound() : NoContent();
    }

    [HttpPut("{id:guid}/skill-selections")]
    [UsesOwnerScopedData]
    public async Task<IActionResult> PutSkillSelections(Guid id, [FromBody] IReadOnlyList<Guid> entryIds, CancellationToken cancellationToken)
    {
        var result = await _replaceSkillSelectionsUseCase.ExecuteAsync(id, entryIds, cancellationToken);
        return result == CVPresentationMutationResult.NotFound ? NotFound() : NoContent();
    }

    [HttpPut("{id:guid}/language-selections")]
    [UsesOwnerScopedData]
    public async Task<IActionResult> PutLanguageSelections(Guid id, [FromBody] IReadOnlyList<Guid> entryIds, CancellationToken cancellationToken)
    {
        var result = await _replaceLanguageSelectionsUseCase.ExecuteAsync(id, entryIds, cancellationToken);
        return result == CVPresentationMutationResult.NotFound ? NotFound() : NoContent();
    }

    [HttpPut("{id:guid}/certification-selections")]
    [UsesOwnerScopedData]
    public async Task<IActionResult> PutCertificationSelections(Guid id, [FromBody] IReadOnlyList<Guid> entryIds, CancellationToken cancellationToken)
    {
        var result = await _replaceCertificationSelectionsUseCase.ExecuteAsync(id, entryIds, cancellationToken);
        return result == CVPresentationMutationResult.NotFound ? NotFound() : NoContent();
    }

    [HttpPut("{id:guid}/project-selections")]
    [UsesOwnerScopedData]
    public async Task<IActionResult> PutProjectSelections(Guid id, [FromBody] IReadOnlyList<Guid> entryIds, CancellationToken cancellationToken)
    {
        var result = await _replaceProjectSelectionsUseCase.ExecuteAsync(id, entryIds, cancellationToken);
        return result == CVPresentationMutationResult.NotFound ? NotFound() : NoContent();
    }

    [HttpPut("{id:guid}/profile-link-selections")]
    [UsesOwnerScopedData]
    public async Task<IActionResult> PutProfileLinkSelections(Guid id, [FromBody] IReadOnlyList<Guid> entryIds, CancellationToken cancellationToken)
    {
        var result = await _replaceProfileLinkSelectionsUseCase.ExecuteAsync(id, entryIds, cancellationToken);
        return result == CVPresentationMutationResult.NotFound ? NotFound() : NoContent();
    }

    // Not [UsesOwnerScopedData] — AnalysisCommandOrchestrator/AnalyzeCVPresentationUseCase open
    // their own short, independently-committed owner-scoped transactions around each DB phase
    // (ADR-0014), so no transaction is held open for the duration of the external Anthropic call.
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

public sealed record CVPresentationCreatedResponse(Guid Id);

public sealed record CreateCVPresentationRequest(
    Guid ProfessionalProfileId,
    string Label,
    string TargetMarket,
    string? TargetRole,
    string Locale,
    string TemplateKey,
    string? SummaryOverrideMarkdown,
    bool IncludePhoto,
    bool IncludeEmail,
    bool IncludePhone,
    bool IncludeAddress,
    string DateFormat,
    int PageLimit)
{
    public Task<Guid?> CreateAsync(CreateCVPresentationUseCase useCase, CancellationToken cancellationToken)
        => useCase.ExecuteAsync(
            ProfessionalProfileId, Label, TargetMarket, TargetRole, Locale, TemplateKey, SummaryOverrideMarkdown,
            IncludePhoto, IncludeEmail, IncludePhone, IncludeAddress, DateFormat, PageLimit, cancellationToken);
}

public sealed record UpdateCVPresentationRequest(
    string Label,
    string TargetMarket,
    string? TargetRole,
    string Locale,
    string TemplateKey,
    string? SummaryOverrideMarkdown,
    bool IncludePhoto,
    bool IncludeEmail,
    bool IncludePhone,
    bool IncludeAddress,
    string DateFormat,
    int PageLimit)
{
    public Task<CVPresentationMutationResult> UpdateAsync(UpdateCVPresentationUseCase useCase, Guid id, CancellationToken cancellationToken)
        => useCase.ExecuteAsync(
            id, Label, TargetMarket, TargetRole, Locale, TemplateKey, SummaryOverrideMarkdown,
            IncludePhoto, IncludeEmail, IncludePhone, IncludeAddress, DateFormat, PageLimit, cancellationToken);
}

public sealed record CVPresentationResponse(
    Guid Id,
    Guid ProfessionalProfileId,
    string Label,
    string TargetMarket,
    string? TargetRole,
    string Locale,
    string TemplateKey,
    string? SummaryOverrideMarkdown,
    bool IncludePhoto,
    bool IncludeEmail,
    bool IncludePhone,
    bool IncludeAddress,
    string DateFormat,
    int PageLimit,
    IReadOnlyList<Guid> ExperienceSelections,
    IReadOnlyList<Guid> EducationSelections,
    IReadOnlyList<Guid> SkillSelections,
    IReadOnlyList<Guid> LanguageSelections,
    IReadOnlyList<Guid> CertificationSelections,
    IReadOnlyList<Guid> ProjectSelections,
    IReadOnlyList<Guid> ProfileLinkSelections,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc)
{
    public static CVPresentationResponse FromResult(CVPresentationResult result) => new(
        result.Id,
        result.ProfessionalProfileId,
        result.Label,
        result.TargetMarket,
        result.TargetRole,
        result.Locale,
        result.TemplateKey,
        result.SummaryOverrideMarkdown,
        result.IncludePhoto,
        result.IncludeEmail,
        result.IncludePhone,
        result.IncludeAddress,
        result.DateFormat,
        result.PageLimit,
        result.ExperienceSelections,
        result.EducationSelections,
        result.SkillSelections,
        result.LanguageSelections,
        result.CertificationSelections,
        result.ProjectSelections,
        result.ProfileLinkSelections,
        result.CreatedAtUtc,
        result.UpdatedAtUtc);
}
