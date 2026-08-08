using CommitAhead.Domain.AIUsage;

namespace CommitAhead.Application.AI;

/// <summary>
/// The backend abstraction over the AI provider (CONTEXT.md) — the only boundary at which real AI
/// calls occur. Never called from the frontend or Domain layer. <c>FakeAIProvider</c> (a
/// deterministic handwritten implementation, ADR-0009) is the only implementation used in
/// automated tests and CI; <c>ProviderAIAdapter</c> (Infrastructure, Anthropic Claude Haiku 4.5 —
/// ADR-0019) is the only implementation that ever makes a real network call.
/// </summary>
public interface IAIProvider
{
    /// <summary>
    /// This provider's own execution metadata for <paramref name="commandType"/> — read by the
    /// analyzing use case before any reservation or call, never hardcoded there.
    /// </summary>
    AiProviderDescriptor Describe(AiCommandType commandType);

    Task<AiAnalysisResult> AnalyzeJobAnalysisAsync(JobAnalysisAiInput input, AiCallLimits limits, CancellationToken cancellationToken);

    Task<AiAnalysisResult> AnalyzeCVPresentationAsync(CVPresentationAiInput input, AiCallLimits limits, CancellationToken cancellationToken);

    Task<AiAnalysisResult> AnalyzeInterviewNoteAsync(InterviewNoteAiInput input, AiCallLimits limits, CancellationToken cancellationToken);
}
