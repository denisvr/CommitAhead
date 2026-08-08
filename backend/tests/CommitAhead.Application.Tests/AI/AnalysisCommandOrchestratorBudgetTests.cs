using CommitAhead.Application.AI;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Application.Tests.JobAnalyses;
using CommitAhead.Application.Tests.ProfessionalProfiles;
using CommitAhead.Application.Tests.StudyItems;
using CommitAhead.Domain.AIUsage;
using CommitAhead.Domain.EvidenceLinks;
using CommitAhead.Domain.JobAnalyses;
using Microsoft.Extensions.Logging.Abstractions;

namespace CommitAhead.Application.Tests.AI;

/// <summary>ADR-0019's daily/monthly per-owner budget check, exercised via AnalyzeJobAnalysisUseCase (any AnalyzeX use case shares the same AnalysisCommandOrchestrator lifecycle).</summary>
public class AnalysisCommandOrchestratorBudgetTests
{
    private static AnalyzeJobAnalysisUseCase CreateUseCase(
        FakeJobAnalysisRepository jobAnalysisRepository, FakeAnalysisDraftRepository draftRepository, FakeAIUsageRecordRepository usageRepository,
        ScriptedAIProvider provider, Guid ownerUserId) => new(
        jobAnalysisRepository, draftRepository, usageRepository, new FakeStudyItemRepository(), new FakeProfessionalProfileRepository(),
        provider, new FakeUnitOfWork(), new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" },
        NullLogger<AnalyzeJobAnalysisUseCase>.Instance);

    private static JobAnalysis CreateJobAnalysis(Guid ownerUserId) =>
        new(Guid.NewGuid(), ownerUserId, "Senior Backend Engineer", new PastedText("Job posting."), null, DateTime.UtcNow);

    private static AIUsageRecord CreateCompletedRecord(Guid ownerUserId, decimal actualCost, DateTime startedAtUtc)
    {
        var record = new AIUsageRecord(
            Guid.NewGuid(), ownerUserId, Guid.NewGuid().ToString(), AiCommandType.AnalyzeJobAnalysis, EvidenceSourceType.JobAnalysis, Guid.NewGuid(),
            "fake", "fake-test-model", "fake-v1", "USD", 1000, 500, actualCost, startedAtUtc);
        record.Complete(100, 50, actualCost, Guid.NewGuid(), "success", startedAtUtc);
        return record;
    }

    [Fact]
    public async Task ExecuteAsync_WhenTodaysSpendPlusEstimateWouldExceedTheDailyLimit_ReturnsDailyBudgetExceeded_WithoutCallingTheProvider()
    {
        var ownerUserId = Guid.NewGuid();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        await jobAnalysisRepository.AddAsync(CreateJobAnalysis(ownerUserId), CancellationToken.None);
        var usageRepository = new FakeAIUsageRecordRepository();
        var nowUtc = DateTime.UtcNow;
        await usageRepository.AddAsync(CreateCompletedRecord(ownerUserId, 0.20m, nowUtc), CancellationToken.None);

        var provider = new ScriptedAIProvider
        {
            Descriptor = new(
            Provider: "fake", Model: "fake-test-model", PricingVersion: "fake-v1", Currency: "USD",
            MaxInputTokens: 8_000, MaxOutputTokens: 2_000, Timeout: TimeSpan.FromSeconds(30), EstimatedMaxCost: 0.10m)
        };
        var jobAnalysis = jobAnalysisRepository.Analyses.Single();
        var useCase = CreateUseCase(jobAnalysisRepository, new FakeAnalysisDraftRepository(), usageRepository, provider, ownerUserId);

        var result = await useCase.ExecuteAsync(jobAnalysis.Id, "key-1", CancellationToken.None);

        Assert.Equal(AnalyzeCommandOutcome.DailyBudgetExceeded, result.Outcome);
        Assert.Equal(0, provider.CallCount);
        Assert.Single(usageRepository.Records);
    }

    [Fact]
    public async Task ExecuteAsync_WhenThisMonthsSpendPlusEstimateWouldExceedTheMonthlyLimit_ReturnsMonthlyBudgetExceeded_WithoutCallingTheProvider()
    {
        var ownerUserId = Guid.NewGuid();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        await jobAnalysisRepository.AddAsync(CreateJobAnalysis(ownerUserId), CancellationToken.None);
        var usageRepository = new FakeAIUsageRecordRepository();
        var nowUtc = DateTime.UtcNow;
        // Spread across several earlier days this month so the daily check alone would pass, but the monthly total is already at the ceiling.
        var startOfMonthUtc = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        await usageRepository.AddAsync(CreateCompletedRecord(ownerUserId, 4.90m, startOfMonthUtc), CancellationToken.None);

        var provider = new ScriptedAIProvider
        {
            Descriptor = new(
            Provider: "fake", Model: "fake-test-model", PricingVersion: "fake-v1", Currency: "USD",
            MaxInputTokens: 8_000, MaxOutputTokens: 2_000, Timeout: TimeSpan.FromSeconds(30), EstimatedMaxCost: 0.20m)
        };
        var jobAnalysis = jobAnalysisRepository.Analyses.Single();
        var useCase = CreateUseCase(jobAnalysisRepository, new FakeAnalysisDraftRepository(), usageRepository, provider, ownerUserId);

        var result = await useCase.ExecuteAsync(jobAnalysis.Id, "key-1", CancellationToken.None);

        Assert.Equal(AnalyzeCommandOutcome.MonthlyBudgetExceeded, result.Outcome);
        Assert.Equal(0, provider.CallCount);
        Assert.Single(usageRepository.Records);
    }

    [Fact]
    public async Task ExecuteAsync_WhenWellUnderBothBudgets_StillCreatesTheDraft()
    {
        var ownerUserId = Guid.NewGuid();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        await jobAnalysisRepository.AddAsync(CreateJobAnalysis(ownerUserId), CancellationToken.None);
        var usageRepository = new FakeAIUsageRecordRepository();

        var provider = new ScriptedAIProvider
        {
            Descriptor = new(
                Provider: "fake", Model: "fake-test-model", PricingVersion: "fake-v1", Currency: "USD",
                MaxInputTokens: 8_000, MaxOutputTokens: 2_000, Timeout: TimeSpan.FromSeconds(30), EstimatedMaxCost: 0.01m),
            Result = new(SuggestionProposals: [], LinkProposals: [], StudyItemProposals: [], InputTokens: 10, OutputTokens: 5, ActualCost: 0.001m),
        };
        var jobAnalysis = jobAnalysisRepository.Analyses.Single();
        var useCase = CreateUseCase(jobAnalysisRepository, new FakeAnalysisDraftRepository(), usageRepository, provider, ownerUserId);

        var result = await useCase.ExecuteAsync(jobAnalysis.Id, "key-1", CancellationToken.None);

        Assert.Equal(AnalyzeCommandOutcome.Created, result.Outcome);
        Assert.Equal(1, provider.CallCount);
    }
}
