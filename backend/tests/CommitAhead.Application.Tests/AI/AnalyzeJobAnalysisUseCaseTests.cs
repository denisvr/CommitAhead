using CommitAhead.Application.AI;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Application.Tests.JobAnalyses;
using CommitAhead.Application.Tests.ProfessionalProfiles;
using CommitAhead.Application.Tests.StudyItems;
using CommitAhead.Domain.AIUsage;
using CommitAhead.Domain.AnalysisDrafts;
using CommitAhead.Domain.EvidenceLinks;
using CommitAhead.Domain.JobAnalyses;
using CommitAhead.Domain.ProfessionalProfiles;
using CommitAhead.Domain.StudyItems;
using Microsoft.Extensions.Logging.Abstractions;

namespace CommitAhead.Application.Tests.AI;

public class AnalyzeJobAnalysisUseCaseTests
{
    private static AnalyzeJobAnalysisUseCase CreateUseCase(
        FakeJobAnalysisRepository jobAnalysisRepository,
        FakeAnalysisDraftRepository draftRepository,
        FakeAIUsageRecordRepository usageRepository,
        IAIProvider aiProvider,
        Guid ownerUserId,
        FakeStudyItemRepository? studyItemRepository = null,
        FakeProfessionalProfileRepository? profileRepository = null)
        => new(
            jobAnalysisRepository,
            draftRepository,
            usageRepository,
            studyItemRepository ?? new FakeStudyItemRepository(),
            profileRepository ?? new FakeProfessionalProfileRepository(),
            aiProvider,
            new FakeUnitOfWork(),
            new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" },
            NullLogger<AnalyzeJobAnalysisUseCase>.Instance);

    private static JobAnalysis CreateJobAnalysis(Guid ownerUserId) =>
        new(Guid.NewGuid(), ownerUserId, "Senior Backend Engineer", new PastedText("We need 5+ years of C# and PostgreSQL."), null, DateTime.UtcNow);

    private static string AddJobRequirementPayload(string proposalKey, string text = "5+ years of C#") =>
        $$"""{"ProposalKey":"{{proposalKey}}","Text":"{{text}}","Kind":"Technical","Priority":"Required","SourceExcerpt":"5+ years of C# required."}""";

    private static string AddJobGapPayload(string? existingRequirementId, string? proposedRequirementKey) =>
        $$"""{"ExistingRequirementId":{{(existingRequirementId is null ? "null" : $"\"{existingRequirementId}\"")}},"ProposedRequirementKey":{{(proposedRequirementKey is null ? "null" : $"\"{proposedRequirementKey}\"")}},"MatchLevel":"Missing","Severity":"High","Rationale":"No PostgreSQL experience found."}""";

    [Fact]
    public async Task ExecuteAsync_WithARequirementAndGapReferencingItByProposalKey_CreatesADraftWithTheAssignedGuid()
    {
        var ownerUserId = Guid.NewGuid();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        var jobAnalysis = CreateJobAnalysis(ownerUserId);
        await jobAnalysisRepository.AddAsync(jobAnalysis, CancellationToken.None);
        var draftRepository = new FakeAnalysisDraftRepository();
        var usageRepository = new FakeAIUsageRecordRepository();
        var provider = new ScriptedAIProvider
        {
            Result = new AiAnalysisResult(
                SuggestionProposals:
                [
                    new AiSuggestionProposal(StructuredSuggestionCommandType.AddJobRequirement, AddJobRequirementPayload("req-1"), null),
                    new AiSuggestionProposal(StructuredSuggestionCommandType.AddJobGap, AddJobGapPayload(null, "req-1"), null),
                ],
                LinkProposals: [],
                StudyItemProposals: [],
                InputTokens: 100,
                OutputTokens: 50,
                ActualCost: 0m),
        };
        var useCase = CreateUseCase(jobAnalysisRepository, draftRepository, usageRepository, provider, ownerUserId);

        var result = await useCase.ExecuteAsync(jobAnalysis.Id, "key-1", CancellationToken.None);

        Assert.Equal(AnalyzeCommandOutcome.Created, result.Outcome);
        var draft = Assert.Single(draftRepository.Drafts);
        Assert.Equal(2, draft.SuggestionProposals.Count);
        var requirementProposal = Assert.Single(draft.SuggestionProposals, p => ((StructuredSuggestion)p.ProposedPayload).CommandType == StructuredSuggestionCommandType.AddJobRequirement);
        var gapProposal = Assert.Single(draft.SuggestionProposals, p => ((StructuredSuggestion)p.ProposedPayload).CommandType == StructuredSuggestionCommandType.AddJobGap);
        var requirementPayload = (StructuredSuggestion)requirementProposal.ProposedPayload;
        var gapPayload = (StructuredSuggestion)gapProposal.ProposedPayload;
        Assert.Contains("AssignedRequirementId", requirementPayload.PayloadJson, StringComparison.Ordinal);
        Assert.DoesNotContain("req-1", gapPayload.PayloadJson, StringComparison.Ordinal);
        var usageRecord = Assert.Single(usageRepository.Records);
        Assert.Equal(AIUsageRecordStatus.Completed, usageRecord.Status);
        Assert.Equal(draft.Id, usageRecord.AnalysisDraftId);
    }

    [Fact]
    public async Task ExecuteAsync_WithAGapReferencingAnExistingRequirement_CreatesTheDraft()
    {
        var ownerUserId = Guid.NewGuid();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        var jobAnalysis = CreateJobAnalysis(ownerUserId);
        var existingRequirement = new JobRequirement(Guid.NewGuid(), "PostgreSQL experience", JobRequirementKind.Technical, JobRequirementPriority.Required, "PostgreSQL required.");
        jobAnalysis.AddRequirement(existingRequirement, DateTime.UtcNow);
        await jobAnalysisRepository.AddAsync(jobAnalysis, CancellationToken.None);
        var draftRepository = new FakeAnalysisDraftRepository();
        var usageRepository = new FakeAIUsageRecordRepository();
        var provider = new ScriptedAIProvider
        {
            Result = new AiAnalysisResult(
                SuggestionProposals: [new AiSuggestionProposal(StructuredSuggestionCommandType.AddJobGap, AddJobGapPayload(existingRequirement.Id.ToString(), null), null)],
                LinkProposals: [],
                StudyItemProposals: [],
                InputTokens: 50,
                OutputTokens: 20,
                ActualCost: 0m),
        };
        var useCase = CreateUseCase(jobAnalysisRepository, draftRepository, usageRepository, provider, ownerUserId);

        var result = await useCase.ExecuteAsync(jobAnalysis.Id, "key-1", CancellationToken.None);

        Assert.Equal(AnalyzeCommandOutcome.Created, result.Outcome);
        var draft = Assert.Single(draftRepository.Drafts);
        var gapPayload = (StructuredSuggestion)Assert.Single(draft.SuggestionProposals).ProposedPayload;
        Assert.Contains(existingRequirement.Id.ToString(), gapPayload.PayloadJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyOutputScenario_CreatesADraftWithNoProposals()
    {
        var ownerUserId = Guid.NewGuid();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        var jobAnalysis = CreateJobAnalysis(ownerUserId);
        await jobAnalysisRepository.AddAsync(jobAnalysis, CancellationToken.None);
        var draftRepository = new FakeAnalysisDraftRepository();
        var usageRepository = new FakeAIUsageRecordRepository();
        var provider = new FakeAIProvider { Scenario = FakeAIScenario.EmptyOutput };
        var useCase = CreateUseCase(jobAnalysisRepository, draftRepository, usageRepository, provider, ownerUserId);

        var result = await useCase.ExecuteAsync(jobAnalysis.Id, "key-1", CancellationToken.None);

        Assert.Equal(AnalyzeCommandOutcome.Created, result.Outcome);
        var draft = Assert.Single(draftRepository.Drafts);
        Assert.Empty(draft.SuggestionProposals);
        Assert.Empty(draft.LinkProposals);
        Assert.Empty(draft.StudyItemProposals);
    }

    [Theory]
    [InlineData(AIUsageRecordStatus.Completed, AnalyzeCommandOutcome.AlreadyCompleted)]
    [InlineData(AIUsageRecordStatus.Reserved, AnalyzeCommandOutcome.InProgress)]
    [InlineData(AIUsageRecordStatus.Failed, AnalyzeCommandOutcome.FailedPreviously)]
    public async Task ExecuteAsync_WithAnExistingRecordForTheSameKey_ReplaysWithoutCallingTheProvider(AIUsageRecordStatus existingStatus, AnalyzeCommandOutcome expectedOutcome)
    {
        var ownerUserId = Guid.NewGuid();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        var jobAnalysis = CreateJobAnalysis(ownerUserId);
        await jobAnalysisRepository.AddAsync(jobAnalysis, CancellationToken.None);
        var draftRepository = new FakeAnalysisDraftRepository();
        var usageRepository = new FakeAIUsageRecordRepository();
        var existingRecord = CreateReservedRecord(ownerUserId, "key-1", jobAnalysis.Id, DateTime.UtcNow);
        TransitionTo(existingRecord, existingStatus);
        await usageRepository.AddAsync(existingRecord, CancellationToken.None);
        var provider = new ScriptedAIProvider();
        var useCase = CreateUseCase(jobAnalysisRepository, draftRepository, usageRepository, provider, ownerUserId);

        var result = await useCase.ExecuteAsync(jobAnalysis.Id, "key-1", CancellationToken.None);

        Assert.Equal(expectedOutcome, result.Outcome);
        Assert.Equal(0, provider.CallCount);
    }

    [Fact]
    public async Task ExecuteAsync_WithAnActiveReservationForAnotherKey_ReturnsAnotherAnalysisInProgressWithoutCallingTheProvider()
    {
        var ownerUserId = Guid.NewGuid();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        var jobAnalysis = CreateJobAnalysis(ownerUserId);
        await jobAnalysisRepository.AddAsync(jobAnalysis, CancellationToken.None);
        var draftRepository = new FakeAnalysisDraftRepository();
        var usageRepository = new FakeAIUsageRecordRepository();
        var activeReservation = CreateReservedRecord(ownerUserId, "other-key", jobAnalysis.Id, DateTime.UtcNow);
        await usageRepository.AddAsync(activeReservation, CancellationToken.None);
        var provider = new ScriptedAIProvider();
        var useCase = CreateUseCase(jobAnalysisRepository, draftRepository, usageRepository, provider, ownerUserId);

        var result = await useCase.ExecuteAsync(jobAnalysis.Id, "new-key", CancellationToken.None);

        Assert.Equal(AnalyzeCommandOutcome.AnotherAnalysisInProgress, result.Outcome);
        Assert.Equal(0, provider.CallCount);
        Assert.Empty(draftRepository.Drafts);
    }

    [Fact]
    public async Task ExecuteAsync_WithAStaleReservationForAnotherKey_ReconcilesItAndProceeds()
    {
        var ownerUserId = Guid.NewGuid();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        var jobAnalysis = CreateJobAnalysis(ownerUserId);
        await jobAnalysisRepository.AddAsync(jobAnalysis, CancellationToken.None);
        var draftRepository = new FakeAnalysisDraftRepository();
        var usageRepository = new FakeAIUsageRecordRepository();
        var staleReservation = CreateReservedRecord(ownerUserId, "stale-key", jobAnalysis.Id, DateTime.UtcNow.AddHours(-1));
        await usageRepository.AddAsync(staleReservation, CancellationToken.None);
        var provider = new FakeAIProvider { Scenario = FakeAIScenario.EmptyOutput };
        var useCase = CreateUseCase(jobAnalysisRepository, draftRepository, usageRepository, provider, ownerUserId);

        var result = await useCase.ExecuteAsync(jobAnalysis.Id, "new-key", CancellationToken.None);

        Assert.Equal(AnalyzeCommandOutcome.Created, result.Outcome);
        Assert.Equal(AIUsageRecordStatus.Failed, staleReservation.Status);
        Assert.Single(draftRepository.Drafts);
    }

    [Fact]
    public async Task ExecuteAsync_WithAnUnknownJobAnalysisId_ReturnsSourceNotFound()
    {
        var ownerUserId = Guid.NewGuid();
        var useCase = CreateUseCase(new FakeJobAnalysisRepository(), new FakeAnalysisDraftRepository(), new FakeAIUsageRecordRepository(), new ScriptedAIProvider(), ownerUserId);

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), "key-1", CancellationToken.None);

        Assert.Equal(AnalyzeCommandOutcome.SourceNotFound, result.Outcome);
    }

    [Fact]
    public async Task ExecuteAsync_WithAnExistingPendingDraftForTheSource_ReturnsDraftAlreadyPending()
    {
        var ownerUserId = Guid.NewGuid();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        var jobAnalysis = CreateJobAnalysis(ownerUserId);
        await jobAnalysisRepository.AddAsync(jobAnalysis, CancellationToken.None);
        var draftRepository = new FakeAnalysisDraftRepository();
        var pendingDraft = new AnalysisDraft(Guid.NewGuid(), ownerUserId, EvidenceSourceType.JobAnalysis, jobAnalysis.Id, [], [], [], DateTime.UtcNow);
        await draftRepository.AddAsync(pendingDraft, CancellationToken.None);
        var provider = new ScriptedAIProvider();
        var useCase = CreateUseCase(jobAnalysisRepository, draftRepository, new FakeAIUsageRecordRepository(), provider, ownerUserId);

        var result = await useCase.ExecuteAsync(jobAnalysis.Id, "key-1", CancellationToken.None);

        Assert.Equal(AnalyzeCommandOutcome.DraftAlreadyPending, result.Outcome);
        Assert.Equal(0, provider.CallCount);
    }

    [Fact]
    public async Task ExecuteAsync_WithMalformedProposalsScenario_FailsTheReservationAndRethrows()
    {
        var ownerUserId = Guid.NewGuid();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        var jobAnalysis = CreateJobAnalysis(ownerUserId);
        await jobAnalysisRepository.AddAsync(jobAnalysis, CancellationToken.None);
        var draftRepository = new FakeAnalysisDraftRepository();
        var usageRepository = new FakeAIUsageRecordRepository();
        var provider = new FakeAIProvider { Scenario = FakeAIScenario.MalformedProposals };
        var useCase = CreateUseCase(jobAnalysisRepository, draftRepository, usageRepository, provider, ownerUserId);

        await Assert.ThrowsAsync<AiResponseValidationException>(() => useCase.ExecuteAsync(jobAnalysis.Id, "key-1", CancellationToken.None));

        Assert.Empty(draftRepository.Drafts);
        var record = Assert.Single(usageRepository.Records);
        Assert.Equal(AIUsageRecordStatus.Failed, record.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WithDuplicatesScenario_FailsTheReservationAndRethrows()
    {
        var ownerUserId = Guid.NewGuid();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        var jobAnalysis = CreateJobAnalysis(ownerUserId);
        await jobAnalysisRepository.AddAsync(jobAnalysis, CancellationToken.None);
        var draftRepository = new FakeAnalysisDraftRepository();
        var usageRepository = new FakeAIUsageRecordRepository();
        var provider = new FakeAIProvider { Scenario = FakeAIScenario.Duplicates };
        var useCase = CreateUseCase(jobAnalysisRepository, draftRepository, usageRepository, provider, ownerUserId);

        await Assert.ThrowsAsync<AiResponseValidationException>(() => useCase.ExecuteAsync(jobAnalysis.Id, "key-1", CancellationToken.None));

        Assert.Empty(draftRepository.Drafts);
        Assert.Equal(AIUsageRecordStatus.Failed, Assert.Single(usageRepository.Records).Status);
    }

    [Fact]
    public async Task ExecuteAsync_WithTimeoutScenario_RethrowsTheOriginalExceptionAndFailsTheReservation()
    {
        var ownerUserId = Guid.NewGuid();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        var jobAnalysis = CreateJobAnalysis(ownerUserId);
        await jobAnalysisRepository.AddAsync(jobAnalysis, CancellationToken.None);
        var draftRepository = new FakeAnalysisDraftRepository();
        var usageRepository = new FakeAIUsageRecordRepository();
        var provider = new FakeAIProvider { Scenario = FakeAIScenario.Timeout };
        var useCase = CreateUseCase(jobAnalysisRepository, draftRepository, usageRepository, provider, ownerUserId);

        await Assert.ThrowsAsync<TimeoutException>(() => useCase.ExecuteAsync(jobAnalysis.Id, "key-1", CancellationToken.None));

        Assert.Equal(AIUsageRecordStatus.Failed, Assert.Single(usageRepository.Records).Status);
    }

    [Fact]
    public async Task ExecuteAsync_WithProviderFailureScenario_RethrowsTheOriginalExceptionAndFailsTheReservation()
    {
        var ownerUserId = Guid.NewGuid();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        var jobAnalysis = CreateJobAnalysis(ownerUserId);
        await jobAnalysisRepository.AddAsync(jobAnalysis, CancellationToken.None);
        var draftRepository = new FakeAnalysisDraftRepository();
        var usageRepository = new FakeAIUsageRecordRepository();
        var provider = new FakeAIProvider { Scenario = FakeAIScenario.ProviderFailure };
        var useCase = CreateUseCase(jobAnalysisRepository, draftRepository, usageRepository, provider, ownerUserId);

        await Assert.ThrowsAsync<HttpRequestException>(() => useCase.ExecuteAsync(jobAnalysis.Id, "key-1", CancellationToken.None));

        Assert.Equal(AIUsageRecordStatus.Failed, Assert.Single(usageRepository.Records).Status);
    }

    [Fact]
    public async Task ExecuteAsync_WithAnUnsupportedCommandType_ThrowsAiResponseValidationException()
    {
        var ownerUserId = Guid.NewGuid();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        var jobAnalysis = CreateJobAnalysis(ownerUserId);
        await jobAnalysisRepository.AddAsync(jobAnalysis, CancellationToken.None);
        var provider = new ScriptedAIProvider
        {
            Result = new AiAnalysisResult(
                SuggestionProposals: [new AiSuggestionProposal(StructuredSuggestionCommandType.UpdateCVPresentationSummary, "{}", null)],
                LinkProposals: [],
                StudyItemProposals: [],
                InputTokens: 10,
                OutputTokens: 10,
                ActualCost: 0m),
        };
        var useCase = CreateUseCase(jobAnalysisRepository, new FakeAnalysisDraftRepository(), new FakeAIUsageRecordRepository(), provider, ownerUserId);

        await Assert.ThrowsAsync<AiResponseValidationException>(() => useCase.ExecuteAsync(jobAnalysis.Id, "key-1", CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_WithADuplicateProposalKey_ThrowsAiResponseValidationException()
    {
        var ownerUserId = Guid.NewGuid();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        var jobAnalysis = CreateJobAnalysis(ownerUserId);
        await jobAnalysisRepository.AddAsync(jobAnalysis, CancellationToken.None);
        var provider = new ScriptedAIProvider
        {
            Result = new AiAnalysisResult(
                SuggestionProposals:
                [
                    new AiSuggestionProposal(StructuredSuggestionCommandType.AddJobRequirement, AddJobRequirementPayload("dup-key"), null),
                    new AiSuggestionProposal(StructuredSuggestionCommandType.AddJobRequirement, AddJobRequirementPayload("dup-key"), null),
                ],
                LinkProposals: [],
                StudyItemProposals: [],
                InputTokens: 10,
                OutputTokens: 10,
                ActualCost: 0m),
        };
        var useCase = CreateUseCase(jobAnalysisRepository, new FakeAnalysisDraftRepository(), new FakeAIUsageRecordRepository(), provider, ownerUserId);

        await Assert.ThrowsAsync<AiResponseValidationException>(() => useCase.ExecuteAsync(jobAnalysis.Id, "key-1", CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_WithAnUnresolvableProposedRequirementKey_ThrowsAiResponseValidationException()
    {
        var ownerUserId = Guid.NewGuid();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        var jobAnalysis = CreateJobAnalysis(ownerUserId);
        await jobAnalysisRepository.AddAsync(jobAnalysis, CancellationToken.None);
        var provider = new ScriptedAIProvider
        {
            Result = new AiAnalysisResult(
                SuggestionProposals: [new AiSuggestionProposal(StructuredSuggestionCommandType.AddJobGap, AddJobGapPayload(null, "no-such-key"), null)],
                LinkProposals: [],
                StudyItemProposals: [],
                InputTokens: 10,
                OutputTokens: 10,
                ActualCost: 0m),
        };
        var useCase = CreateUseCase(jobAnalysisRepository, new FakeAnalysisDraftRepository(), new FakeAIUsageRecordRepository(), provider, ownerUserId);

        await Assert.ThrowsAsync<AiResponseValidationException>(() => useCase.ExecuteAsync(jobAnalysis.Id, "key-1", CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_WithAnUnknownPropertyInPayloadJson_ThrowsAiResponseValidationException()
    {
        var ownerUserId = Guid.NewGuid();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        var jobAnalysis = CreateJobAnalysis(ownerUserId);
        await jobAnalysisRepository.AddAsync(jobAnalysis, CancellationToken.None);
        const string payloadWithUnknownProperty =
            """{"ProposalKey":"req-1","Text":"5+ years of C#","Kind":"Technical","Priority":"Required","SourceExcerpt":"...","Unexpected":"value"}""";
        var provider = new ScriptedAIProvider
        {
            Result = new AiAnalysisResult(
                SuggestionProposals: [new AiSuggestionProposal(StructuredSuggestionCommandType.AddJobRequirement, payloadWithUnknownProperty, null)],
                LinkProposals: [],
                StudyItemProposals: [],
                InputTokens: 10,
                OutputTokens: 10,
                ActualCost: 0m),
        };
        var useCase = CreateUseCase(jobAnalysisRepository, new FakeAnalysisDraftRepository(), new FakeAIUsageRecordRepository(), provider, ownerUserId);

        await Assert.ThrowsAsync<AiResponseValidationException>(() => useCase.ExecuteAsync(jobAnalysis.Id, "key-1", CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_WithANullSuggestionProposalEntry_ThrowsAiResponseValidationException()
    {
        var ownerUserId = Guid.NewGuid();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        var jobAnalysis = CreateJobAnalysis(ownerUserId);
        await jobAnalysisRepository.AddAsync(jobAnalysis, CancellationToken.None);
        var provider = new ScriptedAIProvider
        {
            Result = new AiAnalysisResult(
                SuggestionProposals: [null!],
                LinkProposals: [],
                StudyItemProposals: [],
                InputTokens: 10,
                OutputTokens: 10,
                ActualCost: 0m),
        };
        var useCase = CreateUseCase(jobAnalysisRepository, new FakeAnalysisDraftRepository(), new FakeAIUsageRecordRepository(), provider, ownerUserId);

        await Assert.ThrowsAsync<AiResponseValidationException>(() => useCase.ExecuteAsync(jobAnalysis.Id, "key-1", CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_WithNoProfessionalProfileSaved_SendsAnEmptyProfileSkillsList()
    {
        var ownerUserId = Guid.NewGuid();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        var jobAnalysis = CreateJobAnalysis(ownerUserId);
        await jobAnalysisRepository.AddAsync(jobAnalysis, CancellationToken.None);
        var provider = new ScriptedAIProvider { Result = new AiAnalysisResult([], [], [], 10, 10, 0m) };
        var useCase = CreateUseCase(jobAnalysisRepository, new FakeAnalysisDraftRepository(), new FakeAIUsageRecordRepository(), provider, ownerUserId);

        await useCase.ExecuteAsync(jobAnalysis.Id, "key-1", CancellationToken.None);

        Assert.Empty(provider.LastJobAnalysisInput!.ProfileSkills);
    }

    [Fact]
    public async Task ExecuteAsync_SendsOnlyTheMinimisedInput_ActiveStudyItemsAndProfileSkillNamesOnly()
    {
        var ownerUserId = Guid.NewGuid();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        var jobAnalysis = CreateJobAnalysis(ownerUserId);
        await jobAnalysisRepository.AddAsync(jobAnalysis, CancellationToken.None);

        var profileRepository = new FakeProfessionalProfileRepository();
        var profile = new ProfessionalProfile(Guid.NewGuid(), ownerUserId, new ContactInfo("Owner", "owner@example.com", null, null, null), "Summary.", DateTime.UtcNow);
        profile.ReplaceSkills([new Skill(Guid.NewGuid(), "C#", SkillCategory.Language, null)], DateTime.UtcNow);
        await profileRepository.AddAsync(profile, CancellationToken.None);

        var studyItemRepository = new FakeStudyItemRepository();
        var activeItem = new StudyItem(Guid.NewGuid(), ownerUserId, "Consistent Hashing", StudyItemCategory.Theory, 3, 2, ["distributed-systems"], new TheoryDetails("Summary", ["Point"], ["Question?"], ["https://example.com"]), DateTime.UtcNow);
        var archivedItem = new StudyItem(Guid.NewGuid(), ownerUserId, "Archived Topic", StudyItemCategory.Theory, 3, 2, [], new TheoryDetails("Summary", ["Point"], ["Question?"], ["https://example.com"]), DateTime.UtcNow);
        archivedItem.Archive(DateTime.UtcNow);
        await studyItemRepository.AddAsync(activeItem, CancellationToken.None);
        await studyItemRepository.AddAsync(archivedItem, CancellationToken.None);

        var provider = new ScriptedAIProvider { Result = new AiAnalysisResult([], [], [], 10, 10, 0m) };
        var useCase = CreateUseCase(jobAnalysisRepository, new FakeAnalysisDraftRepository(), new FakeAIUsageRecordRepository(), provider, ownerUserId, studyItemRepository, profileRepository);

        await useCase.ExecuteAsync(jobAnalysis.Id, "key-1", CancellationToken.None);

        var input = provider.LastJobAnalysisInput!;
        Assert.Equal(["C#"], input.ProfileSkills);
        var catalogueEntry = Assert.Single(input.StudyItemCatalogue);
        Assert.Equal(activeItem.Id, catalogueEntry.Id);
        Assert.Equal("We need 5+ years of C# and PostgreSQL.", input.JobPostingText);
        Assert.Empty(input.ExistingRequirements);
    }

    private static AIUsageRecord CreateReservedRecord(Guid ownerUserId, string idempotencyKey, Guid sourceId, DateTime startedAtUtc) => new(
        Guid.NewGuid(), ownerUserId, idempotencyKey, AiCommandType.AnalyzeJobAnalysis, EvidenceSourceType.JobAnalysis, sourceId,
        "fake", "fake-test-model", "fake-v1", "USD", 8_000, 2_000, 0m, startedAtUtc);

    private static void TransitionTo(AIUsageRecord record, AIUsageRecordStatus status)
    {
        switch (status)
        {
            case AIUsageRecordStatus.Reserved:
                break;
            case AIUsageRecordStatus.Completed:
                record.Complete(100, 50, 0m, Guid.NewGuid(), "success", DateTime.UtcNow);
                break;
            case AIUsageRecordStatus.Failed:
                record.Fail("provider-timeout", DateTime.UtcNow);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status));
        }
    }
}
