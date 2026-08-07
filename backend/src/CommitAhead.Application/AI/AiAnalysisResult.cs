namespace CommitAhead.Application.AI;

/// <summary>
/// One AI provider call's raw output. Never a Domain AnalysisDraft directly — the analyzing use
/// case validates every Id/enum/weight/length in here before constructing one (solution.md's AI
/// Analysis Command flow). InputTokens/OutputTokens are the provider's own reported usage, used to
/// reconcile the AIUsageRecord reservation.
/// </summary>
public sealed record AiAnalysisResult(
    IReadOnlyList<AiSuggestionProposal> SuggestionProposals,
    IReadOnlyList<AiLinkProposal> LinkProposals,
    IReadOnlyList<AiStudyItemProposal> StudyItemProposals,
    int InputTokens,
    int OutputTokens,
    decimal ActualCost);
