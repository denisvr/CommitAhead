using CommitAhead.Application.AnalysisDrafts;
using CommitAhead.Application.StudyItems;
using CommitAhead.Application.Tests.AI;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Application.Tests.JobAnalyses;
using CommitAhead.Application.Tests.StudyItems;
using CommitAhead.Domain.AnalysisDrafts;
using CommitAhead.Domain.EvidenceLinks;
using CommitAhead.Domain.JobAnalyses;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Application.Tests.AnalysisDrafts;

public class GetAnalysisDraftUseCaseTests
{
    private static GetAnalysisDraftUseCase CreateUseCase(
        FakeAnalysisDraftRepository repository, Guid ownerUserId, FakeStudyItemRepository? studyItemRepository = null, FakeJobAnalysisRepository? jobAnalysisRepository = null) =>
        new(repository, studyItemRepository ?? new FakeStudyItemRepository(), jobAnalysisRepository ?? new FakeJobAnalysisRepository(), new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" });

    private static StudyItemDetails CreateDetails(StudyItemCategory category) => category switch
    {
        StudyItemCategory.Theory => new TheoryDetails("Summary", ["Point"], ["Question?"], ["https://example.com"]),
        StudyItemCategory.LeetCode => new LeetCodeDetails(42, "https://example.com/42", Difficulty.Medium, ["two-pointers"], "O(n)", "O(1)", "Approach.", null),
        StudyItemCategory.SystemDesign => new SystemDesignDetails("Design a queue.", ["Q?"], ["Functional"], ["Nonfunctional"], ["Check"], "Reference."),
        StudyItemCategory.Behavioral => new BehavioralDetails(["Ownership"], ["Tell me about..."], "Situation", "Task", "Action", "Result", null),
        _ => throw new ArgumentOutOfRangeException(nameof(category)),
    };

    [Fact]
    public async Task ExecuteAsync_WhenNoSuchDraft_ReturnsNull()
    {
        var useCase = CreateUseCase(new FakeAnalysisDraftRepository(), Guid.NewGuid());

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ExecuteAsync_ForAPendingDraft_ProjectsEveryProposalKindIncludingAllFourStudyItemCategories()
    {
        var ownerUserId = Guid.NewGuid();
        var repository = new FakeAnalysisDraftRepository();

        var advisoryProposal = new SuggestionProposal(Guid.NewGuid(), new AdvisorySuggestion("Consider highlighting your PostgreSQL experience."));
        var structuredProposal = new SuggestionProposal(
            Guid.NewGuid(), new StructuredSuggestion(StructuredSuggestionCommandType.AddJobGap, """{"RequirementId":"11111111-1111-1111-1111-111111111111","MatchLevel":"Missing","Severity":"High","Rationale":"No PostgreSQL experience found."}"""));
        var linkProposal = new LinkProposal(Guid.NewGuid(), Guid.NewGuid(), 3, "Directly demonstrates this skill.");
        var studyItemProposals = Enum.GetValues<StudyItemCategory>()
            .Select(category => new StudyItemProposal(Guid.NewGuid(), $"{category} item", category, CreateDetails(category), ["tag"], 3))
            .ToList();

        var draft = new AnalysisDraft(
            Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, Guid.NewGuid(),
            [advisoryProposal, structuredProposal], [linkProposal], studyItemProposals, DateTime.UtcNow);
        await repository.AddAsync(draft, CancellationToken.None);

        var useCase = CreateUseCase(repository, ownerUserId);

        var result = await useCase.ExecuteAsync(draft.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(AnalysisDraftStatus.Pending, result!.Status);
        Assert.Null(result.AppliedAtUtc);

        var advisoryResult = result.SuggestionProposals.Single(p => p.Id == advisoryProposal.Id);
        Assert.Null(advisoryResult.ProposedCommandType);
        Assert.Null(advisoryResult.ProposedPayloadJson);
        Assert.Equal("Consider highlighting your PostgreSQL experience.", advisoryResult.ProposedAdvisoryMarkdown);
        Assert.Null(advisoryResult.AcceptedCommandType);
        Assert.Null(advisoryResult.AcceptedPayloadJson);

        var structuredResult = result.SuggestionProposals.Single(p => p.Id == structuredProposal.Id);
        Assert.Equal(StructuredSuggestionCommandType.AddJobGap, structuredResult.ProposedCommandType);
        Assert.Contains("\"MatchLevel\":\"Missing\"", structuredResult.ProposedPayloadJson);
        Assert.Null(structuredResult.ProposedAdvisoryMarkdown);

        var linkResult = result.LinkProposals.Single();
        Assert.Equal(linkProposal.TargetStudyItemId, linkResult.TargetStudyItemId);
        Assert.Equal(3, linkResult.ProposedWeight);
        Assert.Null(linkResult.AcceptedWeight);

        Assert.Equal(4, result.StudyItemProposals.Count);
        foreach (var category in Enum.GetValues<StudyItemCategory>())
        {
            var studyItemResult = result.StudyItemProposals.Single(p => p.ProposedCategory == category);
            Assert.Equal($"{category} item", studyItemResult.ProposedTitle);

            // Round-trips through the exact same parser the write side (ApplyAnalysisDraftUseCase)
            // already uses — proves the serialized JSON is genuinely re-parseable, not just "looks
            // like JSON".
            var reparsed = StudyItemDetailsJsonParser.Parse(category, studyItemResult.ProposedDetailsJson);
            Assert.IsType(CreateDetails(category).GetType(), reparsed);
        }
    }

    [Fact]
    public async Task ExecuteAsync_ForAnAppliedDraft_AlsoProjectsAcceptedFields()
    {
        var ownerUserId = Guid.NewGuid();
        var repository = new FakeAnalysisDraftRepository();

        var linkProposal = new LinkProposal(Guid.NewGuid(), Guid.NewGuid(), 3, "Directly demonstrates this skill.");
        linkProposal.Accept(4, "Finalised rationale.");

        var draft = new AnalysisDraft(Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, Guid.NewGuid(), [], [linkProposal], [], DateTime.UtcNow);
        draft.MarkApplied(DateTime.UtcNow);
        await repository.AddAsync(draft, CancellationToken.None);

        var useCase = CreateUseCase(repository, ownerUserId);

        var result = await useCase.ExecuteAsync(draft.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(AnalysisDraftStatus.Applied, result!.Status);
        Assert.NotNull(result.AppliedAtUtc);

        var linkResult = result.LinkProposals.Single();
        Assert.Equal(4, linkResult.AcceptedWeight);
        Assert.Equal("Finalised rationale.", linkResult.AcceptedRationale);
    }

    [Fact]
    public async Task ExecuteAsync_ForALinkProposal_ResolvesTheTargetStudyItemsTitle()
    {
        var ownerUserId = Guid.NewGuid();
        var repository = new FakeAnalysisDraftRepository();
        var studyItemRepository = new FakeStudyItemRepository();
        var targetStudyItem = new StudyItem(Guid.NewGuid(), ownerUserId, "PostgreSQL Indexing", StudyItemCategory.Theory, 3, 2, [], new TheoryDetails("s", [], [], []), DateTime.UtcNow);
        await studyItemRepository.AddAsync(targetStudyItem, CancellationToken.None);

        var linkProposal = new LinkProposal(Guid.NewGuid(), targetStudyItem.Id, 3, "Directly demonstrates this skill.");
        var draft = new AnalysisDraft(Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, Guid.NewGuid(), [], [linkProposal], [], DateTime.UtcNow);
        await repository.AddAsync(draft, CancellationToken.None);

        var useCase = CreateUseCase(repository, ownerUserId, studyItemRepository);

        var result = await useCase.ExecuteAsync(draft.Id, CancellationToken.None);

        Assert.Equal("PostgreSQL Indexing", result!.LinkProposals.Single().TargetStudyItemTitle);
    }

    [Fact]
    public async Task ExecuteAsync_ForALinkProposal_WhenTheTargetStudyItemNoLongerExists_LeavesTheTitleNull()
    {
        var ownerUserId = Guid.NewGuid();
        var repository = new FakeAnalysisDraftRepository();
        var linkProposal = new LinkProposal(Guid.NewGuid(), Guid.NewGuid(), 3, "Directly demonstrates this skill.");
        var draft = new AnalysisDraft(Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, Guid.NewGuid(), [], [linkProposal], [], DateTime.UtcNow);
        await repository.AddAsync(draft, CancellationToken.None);

        var useCase = CreateUseCase(repository, ownerUserId);

        var result = await useCase.ExecuteAsync(draft.Id, CancellationToken.None);

        Assert.Null(result!.LinkProposals.Single().TargetStudyItemTitle);
    }

    [Fact]
    public async Task ExecuteAsync_ForAnAddJobGapProposal_ResolvesAnExistingRequirementsText()
    {
        var ownerUserId = Guid.NewGuid();
        var repository = new FakeAnalysisDraftRepository();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();

        var sourceId = Guid.NewGuid();
        var jobAnalysis = new JobAnalysis(sourceId, ownerUserId, "Title", new PastedText("Job posting text."), null, DateTime.UtcNow);
        var requirement = new JobRequirement(Guid.NewGuid(), "5+ years with PostgreSQL", JobRequirementKind.Technical, JobRequirementPriority.Required, "excerpt");
        jobAnalysis.AddRequirement(requirement, DateTime.UtcNow);
        await jobAnalysisRepository.AddAsync(jobAnalysis, CancellationToken.None);

        var gapProposal = new SuggestionProposal(
            Guid.NewGuid(),
            new StructuredSuggestion(StructuredSuggestionCommandType.AddJobGap, $$"""{"RequirementId":"{{requirement.Id}}","MatchLevel":"Missing","Severity":"High","Rationale":"No PostgreSQL experience found."}"""));
        var draft = new AnalysisDraft(Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, sourceId, [gapProposal], [], [], DateTime.UtcNow);
        await repository.AddAsync(draft, CancellationToken.None);

        var useCase = CreateUseCase(repository, ownerUserId, jobAnalysisRepository: jobAnalysisRepository);

        var result = await useCase.ExecuteAsync(draft.Id, CancellationToken.None);

        Assert.Equal("5+ years with PostgreSQL", result!.SuggestionProposals.Single().TargetRequirementText);
    }

    [Fact]
    public async Task ExecuteAsync_ForAnAddJobGapProposal_ResolvesARequirementProposedInTheSameDraft()
    {
        // AddJobGap can reference a sibling AddJobRequirement's AssignedRequirementId — the
        // proposed requirement doesn't exist in the database yet (AiStructuredSuggestionValidator's
        // same-response reference mechanism), so this must resolve from the draft itself, not a
        // repository lookup.
        var ownerUserId = Guid.NewGuid();
        var repository = new FakeAnalysisDraftRepository();

        var assignedRequirementId = Guid.NewGuid();
        var requirementProposal = new SuggestionProposal(
            Guid.NewGuid(),
            new StructuredSuggestion(
                StructuredSuggestionCommandType.AddJobRequirement,
                $$"""{"AssignedRequirementId":"{{assignedRequirementId}}","Text":"Experience with distributed systems","Kind":"Technical","Priority":"Required","SourceExcerpt":"excerpt"}"""));
        var gapProposal = new SuggestionProposal(
            Guid.NewGuid(),
            new StructuredSuggestion(
                StructuredSuggestionCommandType.AddJobGap,
                $$"""{"RequirementId":"{{assignedRequirementId}}","MatchLevel":"Missing","Severity":"High","Rationale":"No distributed-systems experience found."}"""));
        var draft = new AnalysisDraft(Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, Guid.NewGuid(), [requirementProposal, gapProposal], [], [], DateTime.UtcNow);
        await repository.AddAsync(draft, CancellationToken.None);

        var useCase = CreateUseCase(repository, ownerUserId);

        var result = await useCase.ExecuteAsync(draft.Id, CancellationToken.None);

        var gapResult = result!.SuggestionProposals.Single(p => p.ProposedCommandType == StructuredSuggestionCommandType.AddJobGap);
        Assert.Equal("Experience with distributed systems", gapResult.TargetRequirementText);
    }

    [Fact]
    public async Task ExecuteAsync_ForAnotherOwnersDraft_ReturnsNull()
    {
        var repository = new FakeAnalysisDraftRepository();
        var draft = new AnalysisDraft(Guid.NewGuid(), Guid.NewGuid(), EvidenceSourceType.JobAnalysis, Guid.NewGuid(), [], [], [], DateTime.UtcNow);
        await repository.AddAsync(draft, CancellationToken.None);

        var useCase = CreateUseCase(repository, Guid.NewGuid());

        var result = await useCase.ExecuteAsync(draft.Id, CancellationToken.None);

        Assert.Null(result);
    }
}
