using CommitAhead.Application.AI;
using CommitAhead.Application.Tests.CVPresentations;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Application.Tests.ProfessionalProfiles;
using CommitAhead.Application.Tests.StudyItems;
using CommitAhead.Domain.AIUsage;
using CommitAhead.Domain.AnalysisDrafts;
using CommitAhead.Domain.CVPresentations;
using CommitAhead.Domain.ProfessionalProfiles;
using Microsoft.Extensions.Logging.Abstractions;

namespace CommitAhead.Application.Tests.AI;

public class AnalyzeCVPresentationUseCaseTests
{
    private static ContactInfo ValidContactInfo() => new("Ada Lovelace", "ada@example.com", null, null, null);

    private static AnalyzeCVPresentationUseCase CreateUseCase(
        FakeCVPresentationRepository cvRepository,
        FakeAnalysisDraftRepository draftRepository,
        FakeAIUsageRecordRepository usageRepository,
        FakeProfessionalProfileRepository profileRepository,
        IAIProvider aiProvider,
        Guid ownerUserId,
        FakeStudyItemRepository? studyItemRepository = null)
        => new(
            cvRepository,
            draftRepository,
            usageRepository,
            profileRepository,
            studyItemRepository ?? new FakeStudyItemRepository(),
            aiProvider,
            new FakeRlsSessionContext(),
            new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" },
            NullLogger<AnalyzeCVPresentationUseCase>.Instance);

    private static (CVPresentation Presentation, ProfessionalProfile Profile) CreatePresentation(Guid ownerUserId)
    {
        var profile = new ProfessionalProfile(Guid.NewGuid(), ownerUserId, ValidContactInfo(), "Canonical summary.", DateTime.UtcNow);
        var presentation = new CVPresentation(
            Guid.NewGuid(), ownerUserId, profile.Id, "Label", "Market", null, "en-GB", "template", null, false, true, true, false, "dd MMM yyyy", 1, DateTime.UtcNow);
        return (presentation, profile);
    }

    private static string UpdateSummaryPayload(string summaryMarkdown) => $$"""{"SummaryMarkdown":"{{summaryMarkdown}}"}""";

    [Fact]
    public async Task ExecuteAsync_WithAValidUpdateSummaryProposal_CreatesTheDraft()
    {
        var ownerUserId = Guid.NewGuid();
        var cvRepository = new FakeCVPresentationRepository();
        var profileRepository = new FakeProfessionalProfileRepository();
        var (presentation, profile) = CreatePresentation(ownerUserId);
        await profileRepository.AddAsync(profile, CancellationToken.None);
        await cvRepository.AddAsync(presentation, CancellationToken.None);
        var draftRepository = new FakeAnalysisDraftRepository();
        var usageRepository = new FakeAIUsageRecordRepository();
        var provider = new ScriptedAIProvider
        {
            Result = new AiAnalysisResult(
                SuggestionProposals: [new AiSuggestionProposal(StructuredSuggestionCommandType.UpdateCVPresentationSummary, UpdateSummaryPayload("Rewritten summary."), null)],
                LinkProposals: [],
                StudyItemProposals: [],
                InputTokens: 50,
                OutputTokens: 20,
                ActualCost: 0m),
        };
        var useCase = CreateUseCase(cvRepository, draftRepository, usageRepository, profileRepository, provider, ownerUserId);

        var result = await useCase.ExecuteAsync(presentation.Id, "key-1", CancellationToken.None);

        Assert.Equal(AnalyzeCommandOutcome.Created, result.Outcome);
        var draft = Assert.Single(draftRepository.Drafts);
        var payload = (StructuredSuggestion)Assert.Single(draft.SuggestionProposals).ProposedPayload;
        Assert.Equal(StructuredSuggestionCommandType.UpdateCVPresentationSummary, payload.CommandType);
        Assert.Contains("Rewritten summary.", payload.PayloadJson, StringComparison.Ordinal);
        Assert.Equal(AIUsageRecordStatus.Completed, Assert.Single(usageRepository.Records).Status);
    }

    [Fact]
    public async Task ExecuteAsync_WithAnUnsupportedCommandType_ThrowsAiResponseValidationException()
    {
        var ownerUserId = Guid.NewGuid();
        var cvRepository = new FakeCVPresentationRepository();
        var profileRepository = new FakeProfessionalProfileRepository();
        var (presentation, profile) = CreatePresentation(ownerUserId);
        await profileRepository.AddAsync(profile, CancellationToken.None);
        await cvRepository.AddAsync(presentation, CancellationToken.None);
        var provider = new ScriptedAIProvider
        {
            Result = new AiAnalysisResult(
                SuggestionProposals: [new AiSuggestionProposal(StructuredSuggestionCommandType.AddJobRequirement, "{}", null)],
                LinkProposals: [],
                StudyItemProposals: [],
                InputTokens: 10,
                OutputTokens: 10,
                ActualCost: 0m),
        };
        var useCase = CreateUseCase(cvRepository, new FakeAnalysisDraftRepository(), new FakeAIUsageRecordRepository(), profileRepository, provider, ownerUserId);

        await Assert.ThrowsAsync<AiResponseValidationException>(() => useCase.ExecuteAsync(presentation.Id, "key-1", CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_WithATooLongSummary_ThrowsAiResponseValidationException()
    {
        var ownerUserId = Guid.NewGuid();
        var cvRepository = new FakeCVPresentationRepository();
        var profileRepository = new FakeProfessionalProfileRepository();
        var (presentation, profile) = CreatePresentation(ownerUserId);
        await profileRepository.AddAsync(profile, CancellationToken.None);
        await cvRepository.AddAsync(presentation, CancellationToken.None);
        var tooLong = new string('a', 20_001);
        var provider = new ScriptedAIProvider
        {
            Result = new AiAnalysisResult(
                SuggestionProposals: [new AiSuggestionProposal(StructuredSuggestionCommandType.UpdateCVPresentationSummary, UpdateSummaryPayload(tooLong), null)],
                LinkProposals: [],
                StudyItemProposals: [],
                InputTokens: 10,
                OutputTokens: 10,
                ActualCost: 0m),
        };
        var useCase = CreateUseCase(cvRepository, new FakeAnalysisDraftRepository(), new FakeAIUsageRecordRepository(), profileRepository, provider, ownerUserId);

        await Assert.ThrowsAsync<AiResponseValidationException>(() => useCase.ExecuteAsync(presentation.Id, "key-1", CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_WithAnUnknownCVPresentationId_ReturnsSourceNotFound()
    {
        var ownerUserId = Guid.NewGuid();
        var useCase = CreateUseCase(
            new FakeCVPresentationRepository(), new FakeAnalysisDraftRepository(), new FakeAIUsageRecordRepository(),
            new FakeProfessionalProfileRepository(), new ScriptedAIProvider(), ownerUserId);

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), "key-1", CancellationToken.None);

        Assert.Equal(AnalyzeCommandOutcome.SourceNotFound, result.Outcome);
    }

    [Fact]
    public async Task ExecuteAsync_SendsTheResolvedSummaryAndSelectedHighlightsOnly()
    {
        var ownerUserId = Guid.NewGuid();
        var cvRepository = new FakeCVPresentationRepository();
        var profileRepository = new FakeProfessionalProfileRepository();
        var (presentation, profile) = CreatePresentation(ownerUserId);
        var experience = new ExperienceEntry(Guid.NewGuid(), "Acme", null, "Engineer", EmploymentType.Permanent, new YearMonth(2020, 1), null, null, WorkMode.Remote, "Summary", [], []);
        profile.ReplaceExperience([experience], DateTime.UtcNow);
        await profileRepository.AddAsync(profile, CancellationToken.None);
        presentation.ReplaceExperienceSelections([experience.Id], DateTime.UtcNow);
        await cvRepository.AddAsync(presentation, CancellationToken.None);
        var provider = new ScriptedAIProvider { Result = new AiAnalysisResult([], [], [], 10, 10, 0m) };
        var useCase = CreateUseCase(cvRepository, new FakeAnalysisDraftRepository(), new FakeAIUsageRecordRepository(), profileRepository, provider, ownerUserId);

        await useCase.ExecuteAsync(presentation.Id, "key-1", CancellationToken.None);

        var input = provider.LastCVPresentationInput!;
        Assert.Equal("Canonical summary.", input.SummaryMarkdown);
        Assert.Equal(["Engineer at Acme"], input.ExperienceHighlights);
        Assert.Empty(input.EducationHighlights);
        Assert.Empty(input.SkillNames);
    }
}
