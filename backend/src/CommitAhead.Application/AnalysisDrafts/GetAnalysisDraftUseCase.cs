using System.Text.Json;
using CommitAhead.Application.Identity;
using CommitAhead.Application.Json;
using CommitAhead.Domain;
using CommitAhead.Domain.AnalysisDrafts;
using CommitAhead.Domain.EvidenceLinks;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Application.AnalysisDrafts;

public sealed class GetAnalysisDraftUseCase
{
    private readonly IAnalysisDraftRepository _repository;
    private readonly ICurrentUser _currentUser;

    public GetAnalysisDraftUseCase(IAnalysisDraftRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<AnalysisDraftResult?> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var draft = await _repository.GetByIdAsync(_currentUser.UserId, id, cancellationToken);
        return draft is null ? null : AnalysisDraftResult.FromDomain(draft);
    }
}

/// <summary>
/// Read model for one AnalysisDraft (any status — Pending, or Applied/Discarded for audit, per
/// model.md). Payload/details are opaque JSON strings, mirroring exactly how the write side
/// (SuggestionProposalDecision/StudyItemProposalDecision) already treats them — this endpoint's
/// caller already has to know each command/category's field shape to build a valid apply request,
/// so nothing here re-derives per-command response types.
/// </summary>
public sealed record AnalysisDraftResult(
    Guid Id,
    EvidenceSourceType SourceType,
    Guid SourceId,
    AnalysisDraftStatus Status,
    DateTime CreatedAtUtc,
    DateTime? AppliedAtUtc,
    DateTime? DiscardedAtUtc,
    IReadOnlyList<SuggestionProposalResult> SuggestionProposals,
    IReadOnlyList<LinkProposalResult> LinkProposals,
    IReadOnlyList<StudyItemProposalResult> StudyItemProposals)
{
    public static AnalysisDraftResult FromDomain(AnalysisDraft draft) => new(
        draft.Id,
        draft.SourceType,
        draft.SourceId,
        draft.Status,
        draft.CreatedAtUtc,
        draft.AppliedAtUtc,
        draft.DiscardedAtUtc,
        draft.SuggestionProposals.Select(SuggestionProposalResult.FromDomain).ToList(),
        draft.LinkProposals.Select(LinkProposalResult.FromDomain).ToList(),
        draft.StudyItemProposals.Select(StudyItemProposalResult.FromDomain).ToList());
}

public sealed record SuggestionProposalResult(
    Guid Id,
    ProposalStatus Status,
    StructuredSuggestionCommandType? ProposedCommandType,
    string? ProposedPayloadJson,
    string? ProposedAdvisoryMarkdown,
    StructuredSuggestionCommandType? AcceptedCommandType,
    string? AcceptedPayloadJson)
{
    public static SuggestionProposalResult FromDomain(SuggestionProposal proposal)
    {
        var (proposedCommandType, proposedPayloadJson, proposedAdvisoryMarkdown) = Unpack(proposal.ProposedPayload);
        var (acceptedCommandType, acceptedPayloadJson, _) = Unpack(proposal.AcceptedPayload);

        return new SuggestionProposalResult(
            proposal.Id, proposal.Status, proposedCommandType, proposedPayloadJson, proposedAdvisoryMarkdown, acceptedCommandType, acceptedPayloadJson);
    }

    private static (StructuredSuggestionCommandType? CommandType, string? PayloadJson, string? AdvisoryMarkdown) Unpack(SuggestionPayload? payload) => payload switch
    {
        StructuredSuggestion structured => (structured.CommandType, structured.PayloadJson, null),
        AdvisorySuggestion advisory => (null, null, advisory.Markdown),
        null => (null, null, null),
        _ => throw new InvalidOperationException($"Unrecognized SuggestionPayload type '{payload.GetType().Name}'."),
    };
}

public sealed record LinkProposalResult(
    Guid Id,
    ProposalStatus Status,
    Guid TargetStudyItemId,
    decimal ProposedWeight,
    string ProposedRationale,
    decimal? AcceptedWeight,
    string? AcceptedRationale)
{
    public static LinkProposalResult FromDomain(LinkProposal proposal) => new(
        proposal.Id, proposal.Status, proposal.TargetStudyItemId, proposal.ProposedWeight, proposal.ProposedRationale, proposal.AcceptedWeight, proposal.AcceptedRationale);
}

public sealed record StudyItemProposalResult(
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
    public static StudyItemProposalResult FromDomain(StudyItemProposal proposal) => new(
        proposal.Id,
        proposal.Status,
        proposal.ProposedTitle,
        proposal.ProposedCategory,
        SerializeDetails(proposal.ProposedDetails),
        proposal.ProposedTags,
        proposal.ProposedImportance,
        proposal.AcceptedTitle,
        proposal.AcceptedCategory,
        proposal.AcceptedDetails is null ? null : SerializeDetails(proposal.AcceptedDetails),
        proposal.AcceptedTags,
        proposal.AcceptedImportance,
        proposal.AcceptedInitialMastery);

    /// <summary>
    /// StudyItemDetails is typed in memory, not JSON — serializing the runtime subtype (via the
    /// `(object)` cast) with the same StrictJsonOptions StudyItemDetailsJsonParser already parses
    /// with round-trips exactly, since both sides use identical PascalCase property names.
    /// </summary>
    private static string SerializeDetails(StudyItemDetails details) => JsonSerializer.Serialize((object)details, StrictJsonOptions.Strict);
}
