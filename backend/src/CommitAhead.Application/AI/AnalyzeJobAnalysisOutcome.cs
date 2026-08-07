namespace CommitAhead.Application.AI;

/// <summary>The result of one AnalyzeJobAnalysisUseCase call — see AnalyzeJobAnalysisUseCase's own doc-comment for exactly when each applies.</summary>
public enum AnalyzeJobAnalysisOutcome
{
    Created,
    AlreadyCompleted,
    InProgress,
    FailedPreviously,
    AnotherAnalysisInProgress,
    SourceNotFound,
    DraftAlreadyPending,
}
