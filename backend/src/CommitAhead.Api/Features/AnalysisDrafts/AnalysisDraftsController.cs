using CommitAhead.Api.Security;
using CommitAhead.Application.AnalysisDrafts;
using Microsoft.AspNetCore.Mvc;

namespace CommitAhead.Api.Features.AnalysisDrafts;

/// <summary>
/// GET /api/analysis-drafts/{id} (reading a draft's proposals for review before deciding) is the
/// immediately following slice, not implemented here — its response DTO needs real design work
/// (surfacing polymorphic proposed/accepted payloads for editing) that belongs with the UI that
/// consumes it.
/// </summary>
[ApiController]
[Route("api/analysis-drafts")]
[UsesOwnerScopedData]
public sealed class AnalysisDraftsController : ControllerBase
{
    private readonly ApplyAnalysisDraftUseCase _applyUseCase;

    public AnalysisDraftsController(ApplyAnalysisDraftUseCase applyUseCase)
    {
        _applyUseCase = applyUseCase;
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
}

public sealed record ApplyAnalysisDraftRequest(
    IReadOnlyList<SuggestionProposalDecision> SuggestionDecisions,
    IReadOnlyList<LinkProposalDecision> LinkDecisions,
    IReadOnlyList<StudyItemProposalDecision> StudyItemDecisions);
