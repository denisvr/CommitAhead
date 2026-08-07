using CommitAhead.Domain.AnalysisDrafts;

namespace CommitAhead.Application.AI;

/// <summary>The three already-validated Domain proposal collections a validated AiAnalysisResult resolves to — what AnalysisCommandOrchestrator needs to construct the AnalysisDraft.</summary>
internal sealed record AnalysisDraftProposals(
    IReadOnlyList<SuggestionProposal> SuggestionProposals,
    IReadOnlyList<LinkProposal> LinkProposals,
    IReadOnlyList<StudyItemProposal> StudyItemProposals);
