namespace CommitAhead.Domain.AIUsage;

/// <summary>The three explicit AI analysis commands (ADR-0005) — the only actions that ever call IAIProvider.</summary>
public enum AiCommandType
{
    AnalyzeJobAnalysis,
    AnalyzeCVPresentation,
    AnalyzeInterviewNote,
}
