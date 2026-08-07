using CommitAhead.Domain;

namespace CommitAhead.Domain.AnalysisDrafts;

/// <summary>
/// A proposal to change the analysed source (ADR-0005). ProposedPayload is immutable AI output;
/// AcceptedPayload (set only by <see cref="Accept"/>) is a separate, user-finalised copy applied
/// for real — never the same reference, even when the user accepts an AI suggestion unedited, so
/// the original AI output stays available for audit regardless of what was actually applied.
/// </summary>
public sealed class SuggestionProposal
{
    public Guid Id { get; }
    public ProposalStatus Status { get; private set; }
    public SuggestionPayload ProposedPayload { get; }
    public SuggestionPayload? AcceptedPayload { get; private set; }

    public SuggestionProposal(Guid id, SuggestionPayload proposedPayload)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Id is required.");
        }

        Id = id;
        ProposedPayload = proposedPayload ?? throw new DomainValidationException("ProposedPayload is required.");
        Status = ProposalStatus.Pending;
    }

    /// <summary>
    /// An accepted AdvisorySuggestion needs no separate payload (model.md) — <paramref name="acceptedPayload"/>
    /// must be null for it, and must be a StructuredSuggestion (even if identical to the proposed
    /// one) for an accepted StructuredSuggestion.
    /// </summary>
    public void Accept(SuggestionPayload? acceptedPayload)
    {
        EnsurePending();

        switch (ProposedPayload)
        {
            case AdvisorySuggestion when acceptedPayload is not null:
                throw new DomainValidationException("An accepted AdvisorySuggestion must not carry a separate accepted payload.");
            case StructuredSuggestion when acceptedPayload is not StructuredSuggestion:
                throw new DomainValidationException("An accepted StructuredSuggestion requires a finalised StructuredSuggestion payload.");
        }

        AcceptedPayload = acceptedPayload;
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
}
