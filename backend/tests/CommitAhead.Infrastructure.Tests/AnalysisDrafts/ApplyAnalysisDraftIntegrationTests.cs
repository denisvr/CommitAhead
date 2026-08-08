using CommitAhead.Application.AnalysisDrafts;
using CommitAhead.Application.Identity;
using CommitAhead.Application.StudyItems;
using CommitAhead.Domain.AnalysisDrafts;
using CommitAhead.Domain.EvidenceLinks;
using CommitAhead.Domain.JobAnalyses;
using CommitAhead.Domain.StudyItems;
using CommitAhead.Infrastructure.AnalysisDrafts;
using CommitAhead.Infrastructure.CVPresentations;
using CommitAhead.Infrastructure.EvidenceLinks;
using CommitAhead.Infrastructure.InterviewNotes;
using CommitAhead.Infrastructure.JobAnalyses;
using CommitAhead.Infrastructure.Persistence;
using CommitAhead.Infrastructure.StudyItems;
using CommitAhead.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CommitAhead.Infrastructure.Tests.AnalysisDrafts;

/// <summary>
/// End-to-end proofs that only a real database transaction can give: two real
/// ApplyAnalysisDraftUseCase instances racing the same draft, and a real rollback across every
/// repository it touches, using the actual use case — not a hand-simulated repository sequence.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class ApplyAnalysisDraftIntegrationTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private CommitAheadDbContext _dbContext = null!;

    public ApplyAnalysisDraftIntegrationTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        _dbContext = NewDbContext();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private CommitAheadDbContext NewDbContext() => new(new DbContextOptionsBuilder<CommitAheadDbContext>().UseNpgsql(_fixture.ConnectionString).Options);

    private static ApplyAnalysisDraftUseCase BuildUseCase(CommitAheadDbContext dbContext, Guid ownerUserId, IStudyItemRepository? studyItemRepositoryOverride = null) => new(
        new AnalysisDraftRepository(dbContext),
        new JobAnalysisRepository(dbContext),
        new CVPresentationRepository(dbContext),
        new InterviewNoteRepository(dbContext),
        studyItemRepositoryOverride ?? new StudyItemRepository(dbContext),
        new EvidenceLinkRepository(dbContext),
        new EfUnitOfWork(dbContext, NullLogger<EfUnitOfWork>.Instance),
        new StubCurrentUser { UserId = ownerUserId });

    private static StudyItem CreateStudyItem(Guid ownerUserId, string title = "PostgreSQL Indexing") => new(
        Guid.NewGuid(), ownerUserId, title, StudyItemCategory.Theory, 3, 2, ["databases"],
        new TheoryDetails("Summary", ["Point"], ["Question?"], ["https://example.com"]), DateTime.UtcNow);

    private static string AddJobRequirementCanonicalJson(Guid assignedRequirementId) =>
        $$"""{"AssignedRequirementId":"{{assignedRequirementId}}","Text":"5+ years of C#","Kind":"Technical","Priority":"Required","SourceExcerpt":"5+ years of C# required."}""";

    private static string AddJobRequirementDecisionJson() =>
        """{"Text":"5+ years of C# (finalised)","Kind":"Technical","Priority":"Required","SourceExcerpt":"5+ years of C# required."}""";

    private static string TheoryDetailsJson() => """{"SummaryMarkdown":"Summary","KeyPoints":["Point"],"InterviewQuestions":["Question?"],"References":["https://example.com"]}""";

    [Fact]
    public async Task ExecuteAsync_WithTwoConcurrentApplyAttempts_OnlyOneSucceedsAndTheEffectExistsExactlyOnce()
    {
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var jobAnalysis = new JobAnalysis(Guid.NewGuid(), ownerUserId, "Senior Backend Engineer", new PastedText("Job posting."), null, DateTime.UtcNow);
        await new JobAnalysisRepository(_dbContext).AddAsync(jobAnalysis, CancellationToken.None);

        var studyItem = CreateStudyItem(ownerUserId);
        await new StudyItemRepository(_dbContext).AddAsync(studyItem, CancellationToken.None);

        var linkProposal = new LinkProposal(Guid.NewGuid(), studyItem.Id, 3, "Directly demonstrates this skill.");
        var draft = new AnalysisDraft(Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, jobAnalysis.Id, [], [linkProposal], [], DateTime.UtcNow);
        await new AnalysisDraftRepository(_dbContext).AddAsync(draft, CancellationToken.None);

        await using var dbContextA = NewDbContext();
        await using var dbContextB = NewDbContext();
        var useCaseA = BuildUseCase(dbContextA, ownerUserId);
        var useCaseB = BuildUseCase(dbContextB, ownerUserId);

        var decisionA = new LinkProposalDecision(linkProposal.Id, true, 3, "Confirmed by A.");
        var decisionB = new LinkProposalDecision(linkProposal.Id, true, 4, "Confirmed by B.");

        var taskA = useCaseA.ExecuteAsync(draft.Id, [], [decisionA], [], CancellationToken.None);
        var taskB = useCaseB.ExecuteAsync(draft.Id, [], [decisionB], [], CancellationToken.None);
        var results = await Task.WhenAll(taskA, taskB);

        Assert.Single(results, r => r == ApplyAnalysisDraftOutcome.Applied);
        Assert.Single(results, r => r == ApplyAnalysisDraftOutcome.DraftNotPending);

        await using var reloadDbContext = NewDbContext();
        var links = await reloadDbContext.EvidenceLinks.Where(link => link.SourceId == jobAnalysis.Id).ToListAsync();
        Assert.Single(links);
    }

    [Fact]
    public async Task ExecuteAsync_WithALateFailureAfterSeveralEffectsAreStaged_RollsBackEverything()
    {
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var jobAnalysis = new JobAnalysis(Guid.NewGuid(), ownerUserId, "Senior Backend Engineer", new PastedText("We need 5+ years of C# and PostgreSQL."), null, DateTime.UtcNow);
        await new JobAnalysisRepository(_dbContext).AddAsync(jobAnalysis, CancellationToken.None);

        var targetStudyItem = CreateStudyItem(ownerUserId);
        await new StudyItemRepository(_dbContext).AddAsync(targetStudyItem, CancellationToken.None);

        var assignedRequirementId = Guid.NewGuid();
        var requirementProposal = new SuggestionProposal(Guid.NewGuid(), new StructuredSuggestion(StructuredSuggestionCommandType.AddJobRequirement, AddJobRequirementCanonicalJson(assignedRequirementId)));
        var linkProposal = new LinkProposal(Guid.NewGuid(), targetStudyItem.Id, 3, "Directly demonstrates this skill.");
        var studyItemProposal = new StudyItemProposal(Guid.NewGuid(), "Consistent Hashing", StudyItemCategory.Theory, new TheoryDetails("Summary", ["Point"], ["Question?"], ["https://example.com"]), ["tag"], 4);

        var draft = new AnalysisDraft(Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, jobAnalysis.Id, [requirementProposal], [linkProposal], [studyItemProposal], DateTime.UtcNow);
        await new AnalysisDraftRepository(_dbContext).AddAsync(draft, CancellationToken.None);

        // Test-only decorator — calls the real repository (so the StudyItem insert is genuinely
        // staged, uncommitted, in the same transaction) and then throws, simulating a late failure
        // after the JobRequirement mutation and the EvidenceLink insert already happened. No
        // production failure hook involved.
        var decoratedStudyItemRepository = new ThrowingStudyItemRepositoryDecorator(new StudyItemRepository(_dbContext));
        var useCase = BuildUseCase(_dbContext, ownerUserId, decoratedStudyItemRepository);

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(
            draft.Id,
            [new SuggestionProposalDecision(requirementProposal.Id, true, AddJobRequirementDecisionJson())],
            [new LinkProposalDecision(linkProposal.Id, true, 3, "Confirmed.")],
            [new StudyItemProposalDecision(studyItemProposal.Id, true, "Consistent Hashing", StudyItemCategory.Theory, TheoryDetailsJson(), ["tag"], 4, 2)],
            CancellationToken.None));

        await using var reloadDbContext = NewDbContext();

        var reloadedDraft = await new AnalysisDraftRepository(reloadDbContext).GetByIdAsync(ownerUserId, draft.Id, CancellationToken.None);
        Assert.Equal(AnalysisDraftStatus.Pending, reloadedDraft!.Status);
        Assert.All(reloadedDraft.SuggestionProposals, p => Assert.Equal(ProposalStatus.Pending, p.Status));
        Assert.All(reloadedDraft.LinkProposals, p => Assert.Equal(ProposalStatus.Pending, p.Status));
        Assert.All(reloadedDraft.StudyItemProposals, p => Assert.Equal(ProposalStatus.Pending, p.Status));

        var reloadedJobAnalysis = await new JobAnalysisRepository(reloadDbContext).GetByIdAsync(ownerUserId, jobAnalysis.Id, CancellationToken.None);
        Assert.Empty(reloadedJobAnalysis!.Requirements);

        var links = await reloadDbContext.EvidenceLinks.Where(link => link.SourceId == jobAnalysis.Id).ToListAsync();
        Assert.Empty(links);

        var studyItems = await new StudyItemRepository(reloadDbContext).GetAllAsync(ownerUserId, CancellationToken.None);
        Assert.Single(studyItems);
    }

    private sealed class StubCurrentUser : ICurrentUser
    {
        public required Guid UserId { get; init; }

        public string Email => "owner@example.com";
    }

    private sealed class ThrowingStudyItemRepositoryDecorator : IStudyItemRepository
    {
        private readonly IStudyItemRepository _inner;

        public ThrowingStudyItemRepositoryDecorator(IStudyItemRepository inner)
        {
            _inner = inner;
        }

        public Task<StudyItem?> GetByIdAsync(Guid ownerUserId, Guid id, CancellationToken cancellationToken) => _inner.GetByIdAsync(ownerUserId, id, cancellationToken);

        public Task<IReadOnlyList<StudyItem>> GetAllAsync(Guid ownerUserId, CancellationToken cancellationToken) => _inner.GetAllAsync(ownerUserId, cancellationToken);

        public async Task AddAsync(StudyItem item, CancellationToken cancellationToken)
        {
            await _inner.AddAsync(item, cancellationToken);
            throw new InvalidOperationException("Simulated late failure after staging a StudyItem.");
        }

        public Task<bool> DeleteAsync(StudyItem item, CancellationToken cancellationToken) => _inner.DeleteAsync(item, cancellationToken);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => _inner.SaveChangesAsync(cancellationToken);
    }
}
