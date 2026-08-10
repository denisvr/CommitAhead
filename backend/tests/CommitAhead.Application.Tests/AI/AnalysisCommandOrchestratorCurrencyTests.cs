using CommitAhead.Application.AI;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Application.Tests.JobAnalyses;
using CommitAhead.Application.Tests.ProfessionalProfiles;
using CommitAhead.Application.Tests.StudyItems;
using CommitAhead.Domain.JobAnalyses;
using Microsoft.Extensions.Logging.Abstractions;

namespace CommitAhead.Application.Tests.AI;

/// <summary>ADR-0019's budgets are USD-only — a provider describing itself with any other currency must be rejected before any DB write or provider call, defensively, in case a future provider ever forgets this.</summary>
public sealed class AnalysisCommandOrchestratorCurrencyTests
{
    private static AnalyzeJobAnalysisUseCase CreateUseCase(
        FakeJobAnalysisRepository jobAnalysisRepository, FakeAnalysisDraftRepository draftRepository, FakeAIUsageRecordRepository usageRepository,
        ScriptedAIProvider provider, Guid ownerUserId) => new(
        jobAnalysisRepository, draftRepository, usageRepository, new FakeStudyItemRepository(), new FakeProfessionalProfileRepository(),
        provider, new FakeRlsSessionContext(), new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" },
        NullLogger<AnalyzeJobAnalysisUseCase>.Instance);

    [Fact]
    public async Task ExecuteAsync_WhenTheProviderDescriptorsCurrencyIsNotUsd_ThrowsAndNeverCallsTheProviderOrPersistsAReservation()
    {
        var ownerUserId = Guid.NewGuid();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        await jobAnalysisRepository.AddAsync(
            new JobAnalysis(Guid.NewGuid(), ownerUserId, "Senior Backend Engineer", new PastedText("Job posting."), null, DateTime.UtcNow), CancellationToken.None);
        var usageRepository = new FakeAIUsageRecordRepository();

        var provider = new ScriptedAIProvider
        {
            Descriptor = new(
                Provider: "fake", Model: "fake-test-model", PricingVersion: "fake-v1", Currency: "EUR",
                MaxInputTokens: 8_000, MaxOutputTokens: 2_000, Timeout: TimeSpan.FromSeconds(30), EstimatedMaxCost: 0.01m),
        };
        var jobAnalysis = jobAnalysisRepository.Analyses.Single();
        var useCase = CreateUseCase(jobAnalysisRepository, new FakeAnalysisDraftRepository(), usageRepository, provider, ownerUserId);

        await Assert.ThrowsAsync<UnsupportedProviderCurrencyException>(() => useCase.ExecuteAsync(jobAnalysis.Id, "key-1", CancellationToken.None));

        Assert.Equal(0, provider.CallCount);
        Assert.Empty(usageRepository.Records);
    }
}
