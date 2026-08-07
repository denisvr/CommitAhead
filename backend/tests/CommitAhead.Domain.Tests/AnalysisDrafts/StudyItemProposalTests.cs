using CommitAhead.Domain;
using CommitAhead.Domain.AnalysisDrafts;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Domain.Tests.AnalysisDrafts;

public class StudyItemProposalTests
{
    private static TheoryDetails CreateDetails() => new("Summary.", ["Key point."], ["Question?"], ["https://example.com"]);

    private static StudyItemProposal CreateProposal() => new(
        Guid.NewGuid(), "Consistent Hashing", StudyItemCategory.Theory, CreateDetails(), ["distributed-systems"], 4);

    [Fact]
    public void Constructor_WithValidArguments_StartsPendingWithNoAcceptedFields()
    {
        var proposal = CreateProposal();

        Assert.Equal(ProposalStatus.Pending, proposal.Status);
        Assert.Null(proposal.AcceptedTitle);
        Assert.Null(proposal.AcceptedInitialMastery);
    }

    [Fact]
    public void Constructor_WithBlankTitle_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new StudyItemProposal(Guid.NewGuid(), "   ", StudyItemCategory.Theory, CreateDetails(), [], 3));
    }

    [Fact]
    public void Constructor_WithNullDetails_Throws()
    {
        Assert.Throws<DomainValidationException>(() => new StudyItemProposal(Guid.NewGuid(), "Title", StudyItemCategory.Theory, null!, [], 3));
    }

    [Fact]
    public void Accept_SetsAllAcceptedFieldsIncludingInitialMastery_AndTransitions()
    {
        var proposal = CreateProposal();
        var finalDetails = CreateDetails();

        proposal.Accept("Consistent Hashing (edited)", StudyItemCategory.Theory, finalDetails, ["distributed-systems", "hashing"], 5, 2);

        Assert.Equal(ProposalStatus.Accepted, proposal.Status);
        Assert.Equal("Consistent Hashing (edited)", proposal.AcceptedTitle);
        Assert.Equal(StudyItemCategory.Theory, proposal.AcceptedCategory);
        Assert.Same(finalDetails, proposal.AcceptedDetails);
        Assert.Equal(["distributed-systems", "hashing"], proposal.AcceptedTags);
        Assert.Equal(5, proposal.AcceptedImportance);
        Assert.Equal(2, proposal.AcceptedInitialMastery);
    }

    [Fact]
    public void Accept_WithBlankTitle_Throws()
    {
        var proposal = CreateProposal();

        Assert.Throws<DomainValidationException>(() => proposal.Accept("  ", StudyItemCategory.Theory, CreateDetails(), [], 3, 1));
    }

    [Fact]
    public void Reject_TransitionsToRejected()
    {
        var proposal = CreateProposal();

        proposal.Reject();

        Assert.Equal(ProposalStatus.Rejected, proposal.Status);
    }
}
