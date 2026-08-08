namespace CommitAhead.Application.AI;

/// <summary>Per-owner AI spend ceilings (ADR-0019) — checked by AnalysisCommandOrchestrator against IAIUsageRecordRepository.GetSpentCostAsync before reserving a new AIUsageRecord. Not user-editable; changing these is a code change, not a runtime one.</summary>
public static class AiBudgetLimits
{
    public const decimal DailyLimitUsd = 0.25m;
    public const decimal MonthlyLimitUsd = 5.00m;
}
