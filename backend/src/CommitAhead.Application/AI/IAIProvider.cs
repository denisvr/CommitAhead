namespace CommitAhead.Application.AI;

/// <summary>
/// The backend abstraction over the AI provider (CONTEXT.md) — the only boundary at which real AI
/// calls occur. Never called from the frontend or Domain layer. <c>FakeAIProvider</c> (a
/// deterministic handwritten implementation, ADR-0009) is the only implementation used in
/// automated tests and CI; <c>ProviderAIAdapter</c> (Infrastructure, real provider TBD —
/// docs/tbd.md) is the only implementation that ever makes a real network call.
/// </summary>
public interface IAIProvider
{
    Task<AiAnalysisResult> AnalyzeJobAnalysisAsync(JobAnalysisAiInput input, AiCallLimits limits, CancellationToken cancellationToken);

    Task<AiAnalysisResult> AnalyzeCVPresentationAsync(CVPresentationAiInput input, AiCallLimits limits, CancellationToken cancellationToken);

    Task<AiAnalysisResult> AnalyzeInterviewNoteAsync(InterviewNoteAiInput input, AiCallLimits limits, CancellationToken cancellationToken);
}
