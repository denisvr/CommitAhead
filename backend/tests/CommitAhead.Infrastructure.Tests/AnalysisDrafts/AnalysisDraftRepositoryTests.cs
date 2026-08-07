using CommitAhead.Domain.AnalysisDrafts;
using CommitAhead.Domain.EvidenceLinks;
using CommitAhead.Domain.StudyItems;
using CommitAhead.Infrastructure.AnalysisDrafts;
using CommitAhead.Infrastructure.Persistence;
using CommitAhead.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommitAhead.Infrastructure.Tests.AnalysisDrafts;

[Collection(PostgresCollection.Name)]
public sealed class AnalysisDraftRepositoryTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private CommitAheadDbContext _dbContext = null!;

    public AnalysisDraftRepositoryTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<CommitAheadDbContext>().UseNpgsql(_fixture.ConnectionString).Options;
        _dbContext = new CommitAheadDbContext(options);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private static TheoryDetails CreateDetails() => new("Summary.", ["Key point."], ["Question?"], ["https://example.com"]);

    private static AnalysisDraft CreateDraft(Guid ownerUserId, Guid sourceId, ProposalStatus? decideAllAs = null)
    {
        var suggestion = new SuggestionProposal(Guid.NewGuid(), new AdvisorySuggestion("Consider adding more detail."));
        var link = new LinkProposal(Guid.NewGuid(), Guid.NewGuid(), 3, "Directly demonstrates this skill.");
        var studyItem = new StudyItemProposal(Guid.NewGuid(), "Consistent Hashing", StudyItemCategory.Theory, CreateDetails(), ["distributed-systems"], 4);

        if (decideAllAs == ProposalStatus.Accepted)
        {
            suggestion.Accept(null);
            link.Accept(3, "Directly demonstrates this skill.");
            studyItem.Accept("Consistent Hashing", StudyItemCategory.Theory, CreateDetails(), ["distributed-systems"], 4, 2);
        }
        else if (decideAllAs == ProposalStatus.Rejected)
        {
            suggestion.Reject();
            link.Reject();
            studyItem.Reject();
        }

        return new AnalysisDraft(Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, sourceId, [suggestion], [link], [studyItem], DateTime.UtcNow);
    }

    [Fact]
    public async Task AddThenGetById_RoundTripsAllThreeProposalKinds()
    {
        var repository = new AnalysisDraftRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var draft = CreateDraft(ownerUserId, Guid.NewGuid());

        await repository.AddAsync(draft, CancellationToken.None);

        await using var reloadDbContext = new CommitAheadDbContext(new DbContextOptionsBuilder<CommitAheadDbContext>().UseNpgsql(_fixture.ConnectionString).Options);
        var reloaded = await new AnalysisDraftRepository(reloadDbContext).GetByIdAsync(ownerUserId, draft.Id, CancellationToken.None);

        Assert.NotNull(reloaded);
        Assert.Equal(AnalysisDraftStatus.Pending, reloaded.Status);
        var suggestion = Assert.Single(reloaded.SuggestionProposals);
        var advisory = Assert.IsType<AdvisorySuggestion>(suggestion.ProposedPayload);
        Assert.Equal("Consider adding more detail.", advisory.Markdown);
        var link = Assert.Single(reloaded.LinkProposals);
        Assert.Equal(3, link.ProposedWeight);
        var studyItem = Assert.Single(reloaded.StudyItemProposals);
        Assert.Equal("Consistent Hashing", studyItem.ProposedTitle);
        Assert.IsType<TheoryDetails>(studyItem.ProposedDetails);
    }

    [Fact]
    public async Task AddThenGetById_RoundTripsAcceptedStructuredSuggestionPayload()
    {
        var repository = new AnalysisDraftRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var proposed = new StructuredSuggestion(StructuredSuggestionCommandType.AddJobRequirement, "{\"text\":\"5+ years of C#\"}");
        var proposal = new SuggestionProposal(Guid.NewGuid(), proposed);
        var accepted = new StructuredSuggestion(StructuredSuggestionCommandType.AddJobRequirement, "{\"text\":\"6+ years of C#\"}");
        proposal.Accept(accepted);
        var draft = new AnalysisDraft(Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, Guid.NewGuid(), [proposal], [], [], DateTime.UtcNow);

        await repository.AddAsync(draft, CancellationToken.None);

        await using var reloadDbContext = new CommitAheadDbContext(new DbContextOptionsBuilder<CommitAheadDbContext>().UseNpgsql(_fixture.ConnectionString).Options);
        var reloaded = await new AnalysisDraftRepository(reloadDbContext).GetByIdAsync(ownerUserId, draft.Id, CancellationToken.None);

        var reloadedSuggestion = Assert.Single(reloaded!.SuggestionProposals);
        var reloadedAccepted = Assert.IsType<StructuredSuggestion>(reloadedSuggestion.AcceptedPayload);
        Assert.Equal("{\"text\":\"6+ years of C#\"}", reloadedAccepted.PayloadJson);
    }

    [Fact]
    public async Task GetPendingBySourceAsync_ReturnsTheDraft_WhenOneIsPending()
    {
        var repository = new AnalysisDraftRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var sourceId = Guid.NewGuid();
        var draft = CreateDraft(ownerUserId, sourceId);
        await repository.AddAsync(draft, CancellationToken.None);

        var found = await repository.GetPendingBySourceAsync(ownerUserId, EvidenceSourceType.JobAnalysis, sourceId, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(draft.Id, found.Id);
    }

    [Fact]
    public async Task GetPendingBySourceAsync_ReturnsNull_WhenTheOnlyDraftForThatSourceIsApplied()
    {
        var repository = new AnalysisDraftRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var sourceId = Guid.NewGuid();
        var draft = CreateDraft(ownerUserId, sourceId, ProposalStatus.Accepted);
        draft.MarkApplied(DateTime.UtcNow);
        await repository.AddAsync(draft, CancellationToken.None);

        var found = await repository.GetPendingBySourceAsync(ownerUserId, EvidenceSourceType.JobAnalysis, sourceId, CancellationToken.None);

        Assert.Null(found);
    }

    /// <summary>
    /// Proves 007_rls_phase4.sql's partial unique index (AnalysisDraftConfiguration's own
    /// HasFilter), not just the use-case-level GetPendingBySourceAsync check above — inserting a
    /// second Pending draft for the same source directly must fail even bypassing that check.
    /// </summary>
    [Fact]
    public async Task SaveChanges_WithASecondPendingDraftForTheSameSource_ThrowsFromTheDatabaseConstraint()
    {
        var repository = new AnalysisDraftRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var sourceId = Guid.NewGuid();
        await repository.AddAsync(CreateDraft(ownerUserId, sourceId), CancellationToken.None);

        var secondDraft = CreateDraft(ownerUserId, sourceId);
        _dbContext.AnalysisDrafts.Add(secondDraft);

        await Assert.ThrowsAsync<DbUpdateException>(() => _dbContext.SaveChangesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task MarkApplied_PersistsTheStatusAndDecisions()
    {
        var repository = new AnalysisDraftRepository(_dbContext);
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var draft = CreateDraft(ownerUserId, Guid.NewGuid());
        await repository.AddAsync(draft, CancellationToken.None);

        foreach (var proposal in draft.SuggestionProposals)
        {
            proposal.Accept(null);
        }

        foreach (var proposal in draft.LinkProposals)
        {
            proposal.Accept(proposal.ProposedWeight, proposal.ProposedRationale);
        }

        foreach (var proposal in draft.StudyItemProposals)
        {
            proposal.Accept(proposal.ProposedTitle, proposal.ProposedCategory, proposal.ProposedDetails, proposal.ProposedTags, proposal.ProposedImportance, 2);
        }

        draft.MarkApplied(DateTime.UtcNow);
        await repository.SaveChangesAsync(CancellationToken.None);

        await using var reloadDbContext = new CommitAheadDbContext(new DbContextOptionsBuilder<CommitAheadDbContext>().UseNpgsql(_fixture.ConnectionString).Options);
        var reloaded = await new AnalysisDraftRepository(reloadDbContext).GetByIdAsync(ownerUserId, draft.Id, CancellationToken.None);

        Assert.Equal(AnalysisDraftStatus.Applied, reloaded!.Status);
        Assert.All(reloaded.LinkProposals, p => Assert.Equal(ProposalStatus.Accepted, p.Status));
        Assert.All(reloaded.StudyItemProposals, p => Assert.Equal(2, p.AcceptedInitialMastery));
    }
}
