using CommitAhead.Application.AnalysisDrafts;
using CommitAhead.Application.JobAnalyses;
using CommitAhead.Domain.AnalysisDrafts;
using CommitAhead.Domain.EvidenceLinks;
using CommitAhead.Domain.JobAnalyses;
using CommitAhead.Domain.StudyItems;
using CommitAhead.Infrastructure.AnalysisDrafts;
using CommitAhead.Infrastructure.EvidenceLinks;
using CommitAhead.Infrastructure.JobAnalyses;
using CommitAhead.Infrastructure.Persistence;
using CommitAhead.Infrastructure.StudyItems;
using CommitAhead.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CommitAhead.Infrastructure.Tests.JobAnalyses;

/// <summary>
/// ADR-0011 end-to-end proof, using the real DeleteJobAnalysisUseCase against real Postgres: the
/// EvidenceLink, the AnalysisDraft (with its proposal children), and the JobAnalysis itself all
/// disappear together, in one transaction.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class DeleteJobAnalysisUseCaseIntegrationTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private CommitAheadDbContext _dbContext = null!;

    public DeleteJobAnalysisUseCaseIntegrationTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        _dbContext = new CommitAheadDbContext(new DbContextOptionsBuilder<CommitAheadDbContext>().UseNpgsql(_fixture.ConnectionString).Options);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _fixture.ResetAsync();
    }

    [Fact]
    public async Task ExecuteAsync_DeletesTheEvidenceLinkAndAnalysisDraftWithItsProposals_AlongWithTheJobAnalysis()
    {
        var ownerUserId = await TestUsers.CreateAsync(_dbContext);
        var jobAnalysisRepository = new JobAnalysisRepository(_dbContext);
        var evidenceLinkRepository = new EvidenceLinkRepository(_dbContext);
        var analysisDraftRepository = new AnalysisDraftRepository(_dbContext);
        var studyItemRepository = new StudyItemRepository(_dbContext);

        var jobAnalysis = new JobAnalysis(Guid.NewGuid(), ownerUserId, "Senior Backend Engineer", new PastedText("Job posting."), null, DateTime.UtcNow);
        await jobAnalysisRepository.AddAsync(jobAnalysis, CancellationToken.None);

        var studyItem = new StudyItem(
            Guid.NewGuid(), ownerUserId, "PostgreSQL Indexing", StudyItemCategory.Theory, 3, 2, ["databases"],
            new TheoryDetails("Summary", ["Point"], ["Question?"], ["https://example.com"]), DateTime.UtcNow);
        await studyItemRepository.AddAsync(studyItem, CancellationToken.None);

        await evidenceLinkRepository.AddAsync(
            new EvidenceLink(Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, jobAnalysis.Id, studyItem.Id, 3, "Directly demonstrates this skill.", DateTime.UtcNow),
            CancellationToken.None);

        var suggestionProposal = new SuggestionProposal(Guid.NewGuid(), new AdvisorySuggestion("Consider adding more detail."));
        var draft = new AnalysisDraft(Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, jobAnalysis.Id, [suggestionProposal], [], [], DateTime.UtcNow);
        await analysisDraftRepository.AddAsync(draft, CancellationToken.None);

        var useCase = new DeleteJobAnalysisUseCase(
            jobAnalysisRepository, evidenceLinkRepository, analysisDraftRepository, new EfUnitOfWork(_dbContext, NullLogger<EfUnitOfWork>.Instance),
            new NoOpJobPostingStorage(), new StubCurrentUser { UserId = ownerUserId }, NullLogger<DeleteJobAnalysisUseCase>.Instance);

        var result = await useCase.ExecuteAsync(jobAnalysis.Id, CancellationToken.None);

        Assert.Equal(JobAnalysisMutationResult.Success, result);

        await using var reloadDbContext = new CommitAheadDbContext(new DbContextOptionsBuilder<CommitAheadDbContext>().UseNpgsql(_fixture.ConnectionString).Options);
        Assert.Null(await new JobAnalysisRepository(reloadDbContext).GetByIdAsync(ownerUserId, jobAnalysis.Id, CancellationToken.None));
        Assert.False(await new EvidenceLinkRepository(reloadDbContext).ExistsAsync(ownerUserId, EvidenceSourceType.JobAnalysis, jobAnalysis.Id, studyItem.Id, CancellationToken.None));
        Assert.Null(await new AnalysisDraftRepository(reloadDbContext).GetByIdAsync(ownerUserId, draft.Id, CancellationToken.None));

        var remainingSuggestionProposalCount = await reloadDbContext.Database
            .SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM suggestion_proposals WHERE analysis_draft_id = {draft.Id}")
            .SingleAsync();
        Assert.Equal(0, remainingSuggestionProposalCount);
    }

    private sealed class StubCurrentUser : Application.Identity.ICurrentUser
    {
        public required Guid UserId { get; init; }

        public string Email => "owner@example.com";
    }

    private sealed class NoOpJobPostingStorage : IJobPostingStorage
    {
        public Task UploadAsync(string storageObjectKey, Stream content, string mimeType, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task DeleteAsync(string storageObjectKey, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
