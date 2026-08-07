using CommitAhead.Domain;
using CommitAhead.Domain.AnalysisDrafts;
using CommitAhead.Domain.EvidenceLinks;

namespace CommitAhead.Domain.Tests.AnalysisDrafts;

public class AnalysisDraftTests
{
    private static readonly DateTime CreatedAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static AnalysisDraft CreateDraft(
        IReadOnlyList<SuggestionProposal>? suggestionProposals = null,
        IReadOnlyList<LinkProposal>? linkProposals = null,
        IReadOnlyList<StudyItemProposal>? studyItemProposals = null) => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        EvidenceSourceType.JobAnalysis,
        Guid.NewGuid(),
        suggestionProposals ?? [],
        linkProposals ?? [],
        studyItemProposals ?? [],
        CreatedAt);

    [Fact]
    public void Constructor_WithValidArguments_StartsPending()
    {
        var draft = CreateDraft();

        Assert.Equal(AnalysisDraftStatus.Pending, draft.Status);
        Assert.Equal(CreatedAt, draft.CreatedAtUtc);
        Assert.Null(draft.AppliedAtUtc);
        Assert.Null(draft.DiscardedAtUtc);
    }

    [Fact]
    public void Constructor_WithEmptySourceId_Throws()
    {
        Assert.Throws<DomainValidationException>(
            () => new AnalysisDraft(Guid.NewGuid(), Guid.NewGuid(), EvidenceSourceType.JobAnalysis, Guid.Empty, [], [], [], CreatedAt));
    }

    [Fact]
    public void Constructor_WithDuplicateProposalIds_Throws()
    {
        var proposalId = Guid.NewGuid();
        var duplicated = new List<LinkProposal>
        {
            new(proposalId, Guid.NewGuid(), 3, "Rationale."),
            new(proposalId, Guid.NewGuid(), 3, "Rationale."),
        };

        Assert.Throws<DomainValidationException>(() => CreateDraft(linkProposals: duplicated));
    }

    [Fact]
    public void MarkApplied_WithNoProposals_Succeeds()
    {
        var draft = CreateDraft();

        draft.MarkApplied(CreatedAt.AddHours(1));

        Assert.Equal(AnalysisDraftStatus.Applied, draft.Status);
        Assert.Equal(CreatedAt.AddHours(1), draft.AppliedAtUtc);
    }

    [Fact]
    public void MarkApplied_WithAPendingProposal_Throws()
    {
        var link = new LinkProposal(Guid.NewGuid(), Guid.NewGuid(), 3, "Rationale.");
        var draft = CreateDraft(linkProposals: [link]);

        Assert.Throws<DomainValidationException>(() => draft.MarkApplied(CreatedAt.AddHours(1)));
    }

    [Fact]
    public void MarkApplied_WithEveryProposalDecided_Succeeds()
    {
        var link = new LinkProposal(Guid.NewGuid(), Guid.NewGuid(), 3, "Rationale.");
        var suggestion = new SuggestionProposal(Guid.NewGuid(), new AdvisorySuggestion("Text"));
        var draft = CreateDraft(suggestionProposals: [suggestion], linkProposals: [link]);
        link.Accept(3, "Rationale.");
        suggestion.Reject();

        draft.MarkApplied(CreatedAt.AddHours(1));

        Assert.Equal(AnalysisDraftStatus.Applied, draft.Status);
    }

    [Fact]
    public void Discard_TransitionsToDiscarded()
    {
        var draft = CreateDraft();

        draft.Discard(CreatedAt.AddHours(1));

        Assert.Equal(AnalysisDraftStatus.Discarded, draft.Status);
        Assert.Equal(CreatedAt.AddHours(1), draft.DiscardedAtUtc);
    }

    [Fact]
    public void MarkApplied_WhenAlreadyDiscarded_Throws()
    {
        var draft = CreateDraft();
        draft.Discard(CreatedAt.AddHours(1));

        Assert.Throws<DomainValidationException>(() => draft.MarkApplied(CreatedAt.AddHours(2)));
    }
}
