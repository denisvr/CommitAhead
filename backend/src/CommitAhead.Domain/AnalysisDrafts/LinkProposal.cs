using CommitAhead.Domain;

namespace CommitAhead.Domain.AnalysisDrafts;

/// <summary>
/// A proposal to create an EvidenceLink from the analysed source to an existing StudyItem
/// (ADR-0004). TargetStudyItemId is fixed at proposal time — only Weight/Rationale may be
/// finalised on <see cref="Accept"/> (roadmap's "editable final accepted payloads").
/// </summary>
public sealed class LinkProposal
{
    public Guid Id { get; }
    public ProposalStatus Status { get; private set; }
    public Guid TargetStudyItemId { get; }
    public decimal ProposedWeight { get; }
    public string ProposedRationale { get; }
    public decimal? AcceptedWeight { get; private set; }
    public string? AcceptedRationale { get; private set; }

    public LinkProposal(Guid id, Guid targetStudyItemId, decimal proposedWeight, string proposedRationale)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Id is required.");
        }

        if (targetStudyItemId == Guid.Empty)
        {
            throw new DomainValidationException("TargetStudyItemId is required.");
        }

        Id = id;
        TargetStudyItemId = targetStudyItemId;
        ProposedWeight = ValidateWeight(proposedWeight);
        ProposedRationale = TextValidation.RequireNonBlank(proposedRationale, nameof(proposedRationale), ValidationLimits.LinkProposalRationaleMaxLength);
        Status = ProposalStatus.Pending;
    }

    public void Accept(decimal weight, string rationale)
    {
        EnsurePending();

        AcceptedWeight = ValidateWeight(weight);
        AcceptedRationale = TextValidation.RequireNonBlank(rationale, nameof(rationale), ValidationLimits.LinkProposalRationaleMaxLength);
        Status = ProposalStatus.Accepted;
    }

    public void Reject()
    {
        EnsurePending();
        Status = ProposalStatus.Rejected;
    }

    private void EnsurePending()
    {
        if (Status != ProposalStatus.Pending)
        {
            throw new DomainValidationException("Only a Pending proposal can receive a decision.");
        }
    }

    private static decimal ValidateWeight(decimal weight)
    {
        if (weight < 0 || weight > 5)
        {
            throw new DomainValidationException("Weight must be in [0,5].");
        }

        return weight;
    }
}
