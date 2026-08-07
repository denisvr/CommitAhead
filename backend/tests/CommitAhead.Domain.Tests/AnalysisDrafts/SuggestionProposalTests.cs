using CommitAhead.Domain;
using CommitAhead.Domain.AnalysisDrafts;

namespace CommitAhead.Domain.Tests.AnalysisDrafts;

public class SuggestionProposalTests
{
    [Fact]
    public void Constructor_WithValidArguments_StartsPendingWithNoAcceptedPayload()
    {
        var proposal = new SuggestionProposal(Guid.NewGuid(), new AdvisorySuggestion("Consider adding more detail."));

        Assert.Equal(ProposalStatus.Pending, proposal.Status);
        Assert.Null(proposal.AcceptedPayload);
    }

    [Fact]
    public void Constructor_WithEmptyId_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new SuggestionProposal(Guid.Empty, new AdvisorySuggestion("Text")));
    }

    [Fact]
    public void Constructor_WithNullProposedPayload_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new SuggestionProposal(Guid.NewGuid(), null!));
    }

    [Fact]
    public void Accept_AnAdvisorySuggestion_WithNoAcceptedPayload_Succeeds()
    {
        var proposal = new SuggestionProposal(Guid.NewGuid(), new AdvisorySuggestion("Consider adding more detail."));

        proposal.Accept(null);

        Assert.Equal(ProposalStatus.Accepted, proposal.Status);
        Assert.Null(proposal.AcceptedPayload);
    }

    [Fact]
    public void Accept_AnAdvisorySuggestion_WithAnAcceptedPayload_Throws()
    {
        var proposal = new SuggestionProposal(Guid.NewGuid(), new AdvisorySuggestion("Consider adding more detail."));

        Assert.Throws<DomainValidationException>(() => proposal.Accept(new AdvisorySuggestion("Edited text.")));
    }

    [Fact]
    public void Accept_AStructuredSuggestion_WithAFinalisedStructuredSuggestion_Succeeds()
    {
        var proposal = new SuggestionProposal(Guid.NewGuid(), new StructuredSuggestion(StructuredSuggestionCommandType.AddJobRequirement, "{\"text\":\"5+ years of C#\"}"));

        var accepted = new StructuredSuggestion(StructuredSuggestionCommandType.AddJobRequirement, "{\"text\":\"6+ years of C#\"}");
        proposal.Accept(accepted);

        Assert.Equal(ProposalStatus.Accepted, proposal.Status);
        Assert.Same(accepted, proposal.AcceptedPayload);
    }

    [Fact]
    public void Accept_AStructuredSuggestion_WithNoAcceptedPayload_Throws()
    {
        var proposal = new SuggestionProposal(Guid.NewGuid(), new StructuredSuggestion(StructuredSuggestionCommandType.AddJobRequirement, "{}"));

        Assert.Throws<DomainValidationException>(() => proposal.Accept(null));
    }

    [Fact]
    public void Accept_AStructuredSuggestion_WithAnAdvisoryAcceptedPayload_Throws()
    {
        var proposal = new SuggestionProposal(Guid.NewGuid(), new StructuredSuggestion(StructuredSuggestionCommandType.AddJobRequirement, "{}"));

        Assert.Throws<DomainValidationException>(() => proposal.Accept(new AdvisorySuggestion("Not a structured suggestion.")));
    }

    [Fact]
    public void Reject_TransitionsToRejected()
    {
        var proposal = new SuggestionProposal(Guid.NewGuid(), new AdvisorySuggestion("Text"));

        proposal.Reject();

        Assert.Equal(ProposalStatus.Rejected, proposal.Status);
    }

    [Fact]
    public void Accept_WhenAlreadyDecided_Throws()
    {
        var proposal = new SuggestionProposal(Guid.NewGuid(), new AdvisorySuggestion("Text"));
        proposal.Reject();

        Assert.Throws<DomainValidationException>(() => proposal.Accept(null));
    }
}
