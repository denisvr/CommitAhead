using CommitAhead.Application.AI;
using CommitAhead.Domain.AIUsage;

namespace CommitAhead.Application.Tests.AI;

/// <summary>
/// A second, more controllable IAIProvider fake (alongside FakeAIProvider's six fixed scenarios) —
/// lets a test script an exact AiAnalysisResult/AiProviderDescriptor/exception and count calls, for
/// AnalyzeJobAnalysisUseCase tests that need a specific, validator-compatible payload shape rather
/// than one of the six standard scenarios.
/// </summary>
public sealed class ScriptedAIProvider : IAIProvider
{
    public AiProviderDescriptor Descriptor { get; set; } = new(
        Provider: "fake", Model: "fake-test-model", PricingVersion: "fake-v1", Currency: "USD",
        MaxInputTokens: 8_000, MaxOutputTokens: 2_000, Timeout: TimeSpan.FromSeconds(30), EstimatedMaxCost: 0m);

    public AiAnalysisResult? Result { get; set; }

    public Exception? ExceptionToThrow { get; set; }

    public int CallCount { get; private set; }

    public JobAnalysisAiInput? LastInput { get; private set; }

    public AiProviderDescriptor Describe(AiCommandType commandType) => Descriptor;

    public Task<AiAnalysisResult> AnalyzeJobAnalysisAsync(JobAnalysisAiInput input, AiCallLimits limits, CancellationToken cancellationToken)
    {
        CallCount++;
        LastInput = input;

        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        return Task.FromResult(Result ?? throw new InvalidOperationException("ScriptedAIProvider.Result was not set."));
    }

    public Task<AiAnalysisResult> AnalyzeCVPresentationAsync(CVPresentationAiInput input, AiCallLimits limits, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Not used by AnalyzeJobAnalysisUseCase tests.");

    public Task<AiAnalysisResult> AnalyzeInterviewNoteAsync(InterviewNoteAiInput input, AiCallLimits limits, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Not used by AnalyzeJobAnalysisUseCase tests.");
}
