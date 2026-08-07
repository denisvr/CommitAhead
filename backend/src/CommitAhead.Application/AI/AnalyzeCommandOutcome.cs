namespace CommitAhead.Application.AI;

/// <summary>The result of one AnalyzeX use case call (AnalyzeJobAnalysis/AnalyzeCVPresentation/AnalyzeInterviewNote) — shared across all three, since AnalysisCommandOrchestrator's reservation lifecycle is identical for each.</summary>
public enum AnalyzeCommandOutcome
{
    Created,
    AlreadyCompleted,
    InProgress,
    FailedPreviously,
    AnotherAnalysisInProgress,
    SourceNotFound,
    DraftAlreadyPending,
}
