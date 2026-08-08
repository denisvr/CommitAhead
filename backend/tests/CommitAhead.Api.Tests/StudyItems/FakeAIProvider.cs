using CommitAhead.Application.AI;
using CommitAhead.Domain.AIUsage;
using CommitAhead.Domain.AnalysisDrafts;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Api.Tests.StudyItems;

/// <summary>
/// Handwritten deterministic fake (ADR-0009) — Api.Tests-local rather than shared with
/// Application.Tests.AI.FakeAIProvider, since Api.Tests must not take a project reference on
/// Application.Tests just to reuse one class. The only IAIProvider implementation ever exercised
/// in these tests, per CLAUDE.md's "Zero real AI calls in CI" hard constraint.
/// </summary>
public sealed class FakeAIProvider : IAIProvider
{
    public FakeAIScenario Scenario { get; set; } = FakeAIScenario.Success;

    public AiProviderDescriptor Describe(AiCommandType commandType) => new(
        Provider: "fake",
        Model: "fake-test-model",
        PricingVersion: "fake-v1",
        Currency: "USD",
        MaxInputTokens: 8_000,
        MaxOutputTokens: 2_000,
        Timeout: TimeSpan.FromSeconds(30),
        EstimatedMaxCost: 0m);

    public Task<AiAnalysisResult> AnalyzeJobAnalysisAsync(JobAnalysisAiInput input, AiCallLimits limits, CancellationToken cancellationToken) =>
        ProduceResultAsync();

    public Task<AiAnalysisResult> AnalyzeCVPresentationAsync(CVPresentationAiInput input, AiCallLimits limits, CancellationToken cancellationToken) =>
        ProduceResultAsync();

    public Task<AiAnalysisResult> AnalyzeInterviewNoteAsync(InterviewNoteAiInput input, AiCallLimits limits, CancellationToken cancellationToken) =>
        ProduceResultAsync();

    private Task<AiAnalysisResult> ProduceResultAsync() => Scenario switch
    {
        FakeAIScenario.Success => Task.FromResult(SuccessResult()),
        FakeAIScenario.EmptyOutput => Task.FromResult(new AiAnalysisResult([], [], [], InputTokens: 400, OutputTokens: 10, ActualCost: 0m)),
        FakeAIScenario.Timeout => throw new TimeoutException("Simulated provider timeout."),
        FakeAIScenario.ProviderFailure => throw new HttpRequestException("Simulated provider failure."),
        _ => throw new ArgumentOutOfRangeException(nameof(Scenario), Scenario, "Unrecognized scenario."),
    };

    /// <summary>Empty proposal lists — validates successfully regardless of what's in the test database's StudyItem catalogue, unlike a LinkProposal targeting a specific StudyItem Id the test may not have created.</summary>
    private static AiAnalysisResult SuccessResult() => new([], [], [], InputTokens: 500, OutputTokens: 150, ActualCost: 0.001m);
}

public enum FakeAIScenario
{
    Success,
    EmptyOutput,
    Timeout,
    ProviderFailure,
}
