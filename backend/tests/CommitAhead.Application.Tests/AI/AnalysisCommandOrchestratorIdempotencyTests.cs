using CommitAhead.Application.AI;
using CommitAhead.Application.Tests.Identity;
using CommitAhead.Application.Tests.JobAnalyses;
using CommitAhead.Application.Tests.ProfessionalProfiles;
using CommitAhead.Application.Tests.StudyItems;
using CommitAhead.Domain;
using CommitAhead.Domain.JobAnalyses;
using Microsoft.Extensions.Logging.Abstractions;

namespace CommitAhead.Application.Tests.AI;

/// <summary>Idempotency keys are normalized (trimmed, length-validated) once, before any lookup — so " key-1 " and "key-1" resolve to the same replay instead of being silently treated as different keys.</summary>
public class AnalysisCommandOrchestratorIdempotencyTests
{
    private static AnalyzeJobAnalysisUseCase CreateUseCase(
        FakeJobAnalysisRepository jobAnalysisRepository, FakeAnalysisDraftRepository draftRepository, FakeAIUsageRecordRepository usageRepository,
        ScriptedAIProvider provider, Guid ownerUserId) => new(
        jobAnalysisRepository, draftRepository, usageRepository, new FakeStudyItemRepository(), new FakeProfessionalProfileRepository(),
        provider, new FakeUnitOfWork(), new StubCurrentUser { UserId = ownerUserId, Email = "owner@example.com" },
        NullLogger<AnalyzeJobAnalysisUseCase>.Instance);

    private static JobAnalysis CreateJobAnalysis(Guid ownerUserId) =>
        new(Guid.NewGuid(), ownerUserId, "Senior Backend Engineer", new PastedText("Job posting."), null, DateTime.UtcNow);

    [Fact]
    public async Task ExecuteAsync_WithAPaddedKeyMatchingAnEarlierTrimmedKey_ReplaysTheSameRecord_WithoutCallingTheProviderAgain()
    {
        var ownerUserId = Guid.NewGuid();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        await jobAnalysisRepository.AddAsync(CreateJobAnalysis(ownerUserId), CancellationToken.None);
        var jobAnalysis = jobAnalysisRepository.Analyses.Single();
        var usageRepository = new FakeAIUsageRecordRepository();
        var provider = new ScriptedAIProvider
        {
            Result = new(SuggestionProposals: [], LinkProposals: [], StudyItemProposals: [], InputTokens: 10, OutputTokens: 5, ActualCost: 0.001m),
        };
        var useCase = CreateUseCase(jobAnalysisRepository, new FakeAnalysisDraftRepository(), usageRepository, provider, ownerUserId);

        var first = await useCase.ExecuteAsync(jobAnalysis.Id, "key-1", CancellationToken.None);
        var second = await useCase.ExecuteAsync(jobAnalysis.Id, " key-1 ", CancellationToken.None);

        Assert.Equal(AnalyzeCommandOutcome.Created, first.Outcome);
        Assert.Equal(AnalyzeCommandOutcome.AlreadyCompleted, second.Outcome);
        Assert.Equal(first.AnalysisDraftId, second.AnalysisDraftId);
        Assert.Equal(1, provider.CallCount);
        Assert.Single(usageRepository.Records);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExecuteAsync_WithABlankIdempotencyKey_ThrowsDomainValidationException(string blankKey)
    {
        var ownerUserId = Guid.NewGuid();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        await jobAnalysisRepository.AddAsync(CreateJobAnalysis(ownerUserId), CancellationToken.None);
        var jobAnalysis = jobAnalysisRepository.Analyses.Single();
        var useCase = CreateUseCase(jobAnalysisRepository, new FakeAnalysisDraftRepository(), new FakeAIUsageRecordRepository(), new ScriptedAIProvider(), ownerUserId);

        await Assert.ThrowsAsync<DomainValidationException>(() => useCase.ExecuteAsync(jobAnalysis.Id, blankKey, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_WithAnIdempotencyKeyOver200Characters_ThrowsDomainValidationException()
    {
        var ownerUserId = Guid.NewGuid();
        var jobAnalysisRepository = new FakeJobAnalysisRepository();
        await jobAnalysisRepository.AddAsync(CreateJobAnalysis(ownerUserId), CancellationToken.None);
        var jobAnalysis = jobAnalysisRepository.Analyses.Single();
        var useCase = CreateUseCase(jobAnalysisRepository, new FakeAnalysisDraftRepository(), new FakeAIUsageRecordRepository(), new ScriptedAIProvider(), ownerUserId);

        await Assert.ThrowsAsync<DomainValidationException>(() => useCase.ExecuteAsync(jobAnalysis.Id, new string('k', 201), CancellationToken.None));
    }
}
