using CommitAhead.Domain;
using CommitAhead.Domain.AnalysisDrafts;

namespace CommitAhead.Domain.Tests.AnalysisDrafts;

public class LinkProposalTests
{
    [Fact]
    public void Constructor_WithValidArguments_StartsPending()
    {
        var proposal = new LinkProposal(Guid.NewGuid(), Guid.NewGuid(), 3, "Directly demonstrates this skill.");

        Assert.Equal(ProposalStatus.Pending, proposal.Status);
        Assert.Null(proposal.AcceptedWeight);
        Assert.Null(proposal.AcceptedRationale);
    }

    [Fact]
    public void Constructor_WithEmptyTargetStudyItemId_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new LinkProposal(Guid.NewGuid(), Guid.Empty, 3, "Rationale."));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5.01)]
    public void Constructor_WithWeightOutOfRange_Throws(decimal weight)
    {
        Assert.Throws<DomainValidationException>(() => new LinkProposal(Guid.NewGuid(), Guid.NewGuid(), weight, "Rationale."));
    }

    [Fact]
    public void Constructor_WithBlankRationale_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new LinkProposal(Guid.NewGuid(), Guid.NewGuid(), 3, "   "));
    }

    [Fact]
    public void Accept_SetsAcceptedWeightAndRationale_AndTransitions()
    {
        var proposal = new LinkProposal(Guid.NewGuid(), Guid.NewGuid(), 3, "Original rationale.");

        proposal.Accept(4, "Edited rationale.");

        Assert.Equal(ProposalStatus.Accepted, proposal.Status);
        Assert.Equal(4, proposal.AcceptedWeight);
        Assert.Equal("Edited rationale.", proposal.AcceptedRationale);
    }

    [Fact]
    public void Accept_WithWeightOutOfRange_Throws()
    {
        var proposal = new LinkProposal(Guid.NewGuid(), Guid.NewGuid(), 3, "Rationale.");

        Assert.Throws<DomainValidationException>(() => proposal.Accept(6, "Rationale."));
    }

    [Fact]
    public void Reject_TransitionsToRejected()
    {
        var proposal = new LinkProposal(Guid.NewGuid(), Guid.NewGuid(), 3, "Rationale.");

        proposal.Reject();

        Assert.Equal(ProposalStatus.Rejected, proposal.Status);
    }

    [Fact]
    public void Reject_WhenAlreadyDecided_Throws()
    {
        var proposal = new LinkProposal(Guid.NewGuid(), Guid.NewGuid(), 3, "Rationale.");
        proposal.Accept(3, "Rationale.");

        Assert.Throws<DomainValidationException>(proposal.Reject);
    }
}
