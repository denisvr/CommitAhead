using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Application.AnalysisDrafts;

/// <summary>
/// One final decision for a SuggestionProposal. <see cref="AcceptedPayloadJson"/> must be present
/// iff <see cref="Accepted"/> is true (its shape depends on the proposal's own CommandType — see
/// ApplyAnalysisDraftUseCase). For an AdvisorySuggestion proposal, an accepted decision must leave
/// this null (the Domain itself forbids a separate payload for an accepted advisory).
/// </summary>
public sealed record SuggestionProposalDecision(Guid ProposalId, bool Accepted, string? AcceptedPayloadJson);

/// <summary><see cref="Weight"/>/<see cref="Rationale"/> must both be present iff <see cref="Accepted"/> is true.</summary>
public sealed record LinkProposalDecision(Guid ProposalId, bool Accepted, decimal? Weight, string? Rationale);

/// <summary>
/// Every field but <see cref="ProposalId"/>/<see cref="Accepted"/> must be present iff
/// <see cref="Accepted"/> is true. <see cref="InitialMastery"/> is always required for an accepted
/// decision — AI cannot assess it (ADR-0005), so it is never defaulted from the proposal.
/// </summary>
public sealed record StudyItemProposalDecision(
    Guid ProposalId,
    bool Accepted,
    string? Title,
    StudyItemCategory? Category,
    string? DetailsJson,
    IReadOnlyList<string>? Tags,
    int? Importance,
    int? InitialMastery);
