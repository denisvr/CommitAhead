namespace CommitAhead.Application.AI;

/// <summary>Per-call ceilings the use case passes down, independent of the budget reservation itself (docs/tbd.md's "Default AI budgets" — separate concern, still open).</summary>
public sealed record AiCallLimits(int MaxInputTokens, int MaxOutputTokens, TimeSpan Timeout);
