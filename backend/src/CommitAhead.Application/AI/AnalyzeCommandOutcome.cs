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

    /// <summary>The owner's spend for today (Completed actual cost plus active Reserved cost) plus this reservation's estimated max cost would exceed AiBudgetLimits.DailyLimitUsd (ADR-0019).</summary>
    DailyBudgetExceeded,

    /// <summary>Same as DailyBudgetExceeded, checked against the current UTC calendar month and AiBudgetLimits.MonthlyLimitUsd.</summary>
    MonthlyBudgetExceeded,
}
