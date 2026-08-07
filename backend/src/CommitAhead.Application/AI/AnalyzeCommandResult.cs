namespace CommitAhead.Application.AI;

/// <summary><see cref="AnalysisDraftId"/> is set only for <see cref="AnalyzeCommandOutcome.Created"/> and <see cref="AnalyzeCommandOutcome.AlreadyCompleted"/>.</summary>
public sealed record AnalyzeCommandResult(AnalyzeCommandOutcome Outcome, Guid? AnalysisDraftId);
