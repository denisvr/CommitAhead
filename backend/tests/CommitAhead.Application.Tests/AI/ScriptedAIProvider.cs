using CommitAhead.Application.AI;
using CommitAhead.Domain.AIUsage;

namespace CommitAhead.Application.Tests.AI;

/// <summary>
/// A second, more controllable IAIProvider fake (alongside FakeAIProvider's six fixed scenarios) —
/// lets a test script an exact AiAnalysisResult/AiProviderDescriptor/exception and count calls, for
/// AnalyzeX use case tests that need a specific, validator-compatible payload shape rather than one
/// of the six standard scenarios. One shared Result/ExceptionToThrow/CallCount serves whichever of
/// the three Analyze methods the test under it actually calls; each records its own last input.
/// </summary>
public sealed class ScriptedAIProvider : IAIProvider
{
    public AiProviderDescriptor Descriptor { get; set; } = new(
        Provider: "fake", Model: "fake-test-model", PricingVersion: "fake-v1", Currency: "USD",
        MaxInputTokens: 8_000, MaxOutputTokens: 2_000, Timeout: TimeSpan.FromSeconds(30), EstimatedMaxCost: 0m);

    public AiAnalysisResult? Result { get; set; }

    public Exception? ExceptionToThrow { get; set; }

    public int CallCount { get; private set; }

    public JobAnalysisAiInput? LastJobAnalysisInput { get; private set; }

    public CVPresentationAiInput? LastCVPresentationInput { get; private set; }

    public InterviewNoteAiInput? LastInterviewNoteInput { get; private set; }

    public AiProviderDescriptor Describe(AiCommandType commandType) => Descriptor;

    public Task<AiAnalysisResult> AnalyzeJobAnalysisAsync(JobAnalysisAiInput input, AiCallLimits limits, CancellationToken cancellationToken)
    {
        LastJobAnalysisInput = input;
        return Resolve();
    }

    public Task<AiAnalysisResult> AnalyzeCVPresentationAsync(CVPresentationAiInput input, AiCallLimits limits, CancellationToken cancellationToken)
    {
        LastCVPresentationInput = input;
        return Resolve();
    }

    public Task<AiAnalysisResult> AnalyzeInterviewNoteAsync(InterviewNoteAiInput input, AiCallLimits limits, CancellationToken cancellationToken)
    {
        LastInterviewNoteInput = input;
        return Resolve();
    }

    private Task<AiAnalysisResult> Resolve()
    {
        CallCount++;

        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        return Task.FromResult(Result ?? throw new InvalidOperationException("ScriptedAIProvider.Result was not set."));
    }
}
