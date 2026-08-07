namespace CommitAhead.Application.AI;

/// <summary><see cref="AnalysisDraftId"/> is set only for <see cref="AnalyzeJobAnalysisOutcome.Created"/> and <see cref="AnalyzeJobAnalysisOutcome.AlreadyCompleted"/>.</summary>
public sealed record AnalyzeJobAnalysisResult(AnalyzeJobAnalysisOutcome Outcome, Guid? AnalysisDraftId);
