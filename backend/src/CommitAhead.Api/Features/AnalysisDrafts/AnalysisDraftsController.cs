using CommitAhead.Api.Security;
using CommitAhead.Application.AnalysisDrafts;
using CommitAhead.Domain.AnalysisDrafts;
using CommitAhead.Domain.EvidenceLinks;
using CommitAhead.Domain.StudyItems;
using Microsoft.AspNetCore.Mvc;

namespace CommitAhead.Api.Features.AnalysisDrafts;

[ApiController]
[Route("api/analysis-drafts")]
[UsesOwnerScopedData]
public sealed class AnalysisDraftsController : ControllerBase
{
    private readonly GetAnalysisDraftUseCase _getUseCase;
    private readonly ApplyAnalysisDraftUseCase _applyUseCase;
    private readonly DiscardAnalysisDraftUseCase _discardUseCase;

    public AnalysisDraftsController(GetAnalysisDraftUseCase getUseCase, ApplyAnalysisDraftUseCase applyUseCase, DiscardAnalysisDraftUseCase discardUseCase)
    {
        _getUseCase = getUseCase;
        _applyUseCase = applyUseCase;
        _discardUseCase = discardUseCase;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AnalysisDraftResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getUseCase.ExecuteAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(AnalysisDraftResponse.FromResult(result));
    }

    // No rate-limit policy here (unlike the three "analyze" actions) — applying a draft never
    // calls the AI provider.
    [HttpPost("{id:guid}/apply")]
    public async Task<IActionResult> Apply(Guid id, [FromBody] ApplyAnalysisDraftRequest request, CancellationToken cancellationToken)
    {
        var outcome = await _applyUseCase.ExecuteAsync(
            id, request.SuggestionDecisions, request.LinkDecisions, request.StudyItemDecisions, cancellationToken);

        return outcome switch
        {
            ApplyAnalysisDraftOutcome.Applied => NoContent(),
            ApplyAnalysisDraftOutcome.DraftNotFound or ApplyAnalysisDraftOutcome.SourceNotFound => NotFound(),
            _ => AiOutcomeResponses.Conflict(outcome.ToString()),
        };
    }

    // No rate-limit policy here either — discarding never calls the AI provider.
    [HttpPost("{id:guid}/discard")]
    public async Task<IActionResult> Discard(Guid id, CancellationToken cancellationToken)
    {
        var outcome = await _discardUseCase.ExecuteAsync(id, cancellationToken);

        return outcome switch
        {
            DiscardAnalysisDraftOutcome.Discarded => NoContent(),
            DiscardAnalysisDraftOutcome.DraftNotFound => NotFound(),
            _ => AiOutcomeResponses.Conflict(outcome.ToString()),
        };
    }
}

public sealed record ApplyAnalysisDraftRequest(
    IReadOnlyList<SuggestionProposalDecision> SuggestionDecisions,
    IReadOnlyList<LinkProposalDecision> LinkDecisions,
    IReadOnlyList<StudyItemProposalDecision> StudyItemDecisions);

public sealed record AnalysisDraftResponse(
    Guid Id,
    EvidenceSourceType SourceType,
    Guid SourceId,
    AnalysisDraftStatus Status,
    DateTime CreatedAtUtc,
    DateTime? AppliedAtUtc,
    DateTime? DiscardedAtUtc,
    IReadOnlyList<SuggestionProposalResponse> SuggestionProposals,
    IReadOnlyList<LinkProposalResponse> LinkProposals,
    IReadOnlyList<StudyItemProposalResponse> StudyItemProposals)
{
    public static AnalysisDraftResponse FromResult(AnalysisDraftResult result) => new(
        result.Id,
        result.SourceType,
        result.SourceId,
        result.Status,
        result.CreatedAtUtc,
        result.AppliedAtUtc,
        result.DiscardedAtUtc,
        result.SuggestionProposals.Select(SuggestionProposalResponse.FromResult).ToList(),
        result.LinkProposals.Select(LinkProposalResponse.FromResult).ToList(),
        result.StudyItemProposals.Select(StudyItemProposalResponse.FromResult).ToList());
}

public sealed record SuggestionProposalResponse(
    Guid Id,
    ProposalStatus Status,
    StructuredSuggestionCommandType? ProposedCommandType,
    string? ProposedPayloadJson,
    string? ProposedAdvisoryMarkdown,
    StructuredSuggestionCommandType? AcceptedCommandType,
    string? AcceptedPayloadJson,
    /// <summary>Only set for AddJobGap — the text of the JobRequirement it targets, so the review UI never asks the user to decide from a bare RequirementId.</summary>
    string? TargetRequirementText)
{
    public static SuggestionProposalResponse FromResult(SuggestionProposalResult result) => new(
        result.Id, result.Status, result.ProposedCommandType, result.ProposedPayloadJson, result.ProposedAdvisoryMarkdown,
        result.AcceptedCommandType, result.AcceptedPayloadJson, result.TargetRequirementText);
}

public sealed record LinkProposalResponse(
    Guid Id,
    ProposalStatus Status,
    Guid TargetStudyItemId,
    decimal ProposedWeight,
    string ProposedRationale,
    decimal? AcceptedWeight,
    string? AcceptedRationale,
    /// <summary>The target StudyItem's current title, so the review UI never asks the user to decide from a bare Id.</summary>
    string? TargetStudyItemTitle)
{
    public static LinkProposalResponse FromResult(LinkProposalResult result) => new(
        result.Id, result.Status, result.TargetStudyItemId, result.ProposedWeight, result.ProposedRationale, result.AcceptedWeight, result.AcceptedRationale,
        result.TargetStudyItemTitle);
}

public sealed record StudyItemProposalResponse(
    Guid Id,
    ProposalStatus Status,
    string ProposedTitle,
    StudyItemCategory ProposedCategory,
    string ProposedDetailsJson,
    IReadOnlyList<string> ProposedTags,
    int ProposedImportance,
    string? AcceptedTitle,
    StudyItemCategory? AcceptedCategory,
    string? AcceptedDetailsJson,
    IReadOnlyList<string>? AcceptedTags,
    int? AcceptedImportance,
    int? AcceptedInitialMastery)
{
    public static StudyItemProposalResponse FromResult(StudyItemProposalResult result) => new(
        result.Id, result.Status, result.ProposedTitle, result.ProposedCategory, result.ProposedDetailsJson, result.ProposedTags, result.ProposedImportance,
        result.AcceptedTitle, result.AcceptedCategory, result.AcceptedDetailsJson, result.AcceptedTags, result.AcceptedImportance, result.AcceptedInitialMastery);
}
