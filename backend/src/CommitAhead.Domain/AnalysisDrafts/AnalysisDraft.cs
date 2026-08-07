using CommitAhead.Domain;
using CommitAhead.Domain.EvidenceLinks;

namespace CommitAhead.Domain.AnalysisDrafts;

/// <summary>
/// The output of one AI analysis command (ADR-0005). SourceType/SourceId reuse
/// <see cref="EvidenceSourceType"/> — the same three evidence-source kinds an EvidenceLink can
/// point from. At most one Pending AnalysisDraft may exist per (SourceType, SourceId) — enforced
/// by a database unique partial index (Infrastructure), not here; this aggregate has no way to see
/// other AnalysisDraft instances for the same source.
///
/// Applying accepted proposals (creating EvidenceLinks/StudyItems, firing StructuredSuggestion
/// commands) is Application-layer work against those separate aggregates' own repositories — this
/// type only tracks its own proposals' decisions and its own Pending/Applied/Discarded status.
/// </summary>
public sealed class AnalysisDraft
{
    private readonly List<SuggestionProposal> _suggestionProposals = [];
    private readonly List<LinkProposal> _linkProposals = [];
    private readonly List<StudyItemProposal> _studyItemProposals = [];

    public Guid Id { get; }
    public Guid OwnerUserId { get; }
    public EvidenceSourceType SourceType { get; }
    public Guid SourceId { get; }
    public AnalysisDraftStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; }
    public DateTime? AppliedAtUtc { get; private set; }
    public DateTime? DiscardedAtUtc { get; private set; }

    public IReadOnlyList<SuggestionProposal> SuggestionProposals => _suggestionProposals;
    public IReadOnlyList<LinkProposal> LinkProposals => _linkProposals;
    public IReadOnlyList<StudyItemProposal> StudyItemProposals => _studyItemProposals;

    public AnalysisDraft(
        Guid id,
        Guid ownerUserId,
        EvidenceSourceType sourceType,
        Guid sourceId,
        IReadOnlyList<SuggestionProposal> suggestionProposals,
        IReadOnlyList<LinkProposal> linkProposals,
        IReadOnlyList<StudyItemProposal> studyItemProposals,
        DateTime createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Id is required.");
        }

        if (ownerUserId == Guid.Empty)
        {
            throw new DomainValidationException("OwnerUserId is required.");
        }

        if (sourceId == Guid.Empty)
        {
            throw new DomainValidationException("SourceId is required.");
        }

        Id = id;
        OwnerUserId = ownerUserId;
        SourceType = TextValidation.ValidateDefined(sourceType, nameof(sourceType));
        SourceId = sourceId;
        _suggestionProposals = RequireUniqueIds(suggestionProposals, p => p.Id, nameof(suggestionProposals));
        _linkProposals = RequireUniqueIds(linkProposals, p => p.Id, nameof(linkProposals));
        _studyItemProposals = RequireUniqueIds(studyItemProposals, p => p.Id, nameof(studyItemProposals));
        Status = AnalysisDraftStatus.Pending;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>
    /// EF Core materialization only — the public constructor's collection parameters can't be
    /// bound to navigation properties, so this one matches every scalar property instead (EF's
    /// constructor-binding convention matches parameter names to property names), letting EF use
    /// it and then populate the three collection navigations separately via the configured
    /// relationships and their already-empty-initialized backing fields.
    /// </summary>
    private AnalysisDraft(
        Guid id, Guid ownerUserId, EvidenceSourceType sourceType, Guid sourceId, AnalysisDraftStatus status, DateTime createdAtUtc, DateTime? appliedAtUtc, DateTime? discardedAtUtc)
    {
        Id = id;
        OwnerUserId = ownerUserId;
        SourceType = sourceType;
        SourceId = sourceId;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        AppliedAtUtc = appliedAtUtc;
        DiscardedAtUtc = discardedAtUtc;
    }

    /// <summary>
    /// Every proposal across all three collections must already have a decision (model.md) —
    /// callers apply each decision via the proposal's own Accept/Reject *before* calling this, so
    /// by the time this runs the only thing left to check is that none is still Pending.
    /// </summary>
    public void MarkApplied(DateTime appliedAtUtc)
    {
        EnsurePending();

        if (AllProposalStatuses().Any(status => status == ProposalStatus.Pending))
        {
            throw new DomainValidationException("Every proposal must have a decision before the draft can be applied.");
        }

        Status = AnalysisDraftStatus.Applied;
        AppliedAtUtc = appliedAtUtc;
    }

    public void Discard(DateTime discardedAtUtc)
    {
        EnsurePending();

        Status = AnalysisDraftStatus.Discarded;
        DiscardedAtUtc = discardedAtUtc;
    }

    private IEnumerable<ProposalStatus> AllProposalStatuses() =>
        _suggestionProposals.Select(p => p.Status)
            .Concat(_linkProposals.Select(p => p.Status))
            .Concat(_studyItemProposals.Select(p => p.Status));

    private void EnsurePending()
    {
        if (Status != AnalysisDraftStatus.Pending)
        {
            throw new DomainValidationException("Only a Pending draft can transition.");
        }
    }

    private static List<T> RequireUniqueIds<T>(IReadOnlyList<T> proposals, Func<T, Guid> idSelector, string paramName)
    {
        if (proposals is null)
        {
            throw new DomainValidationException($"{paramName} is required.");
        }

        if (proposals.Select(idSelector).Distinct().Count() != proposals.Count)
        {
            throw new DomainValidationException($"{paramName} must not contain duplicate Ids.");
        }

        return [.. proposals];
    }
}
