namespace CommitAhead.Domain.StudyItems;

/// <summary>
/// Pure formula from ADR-0003: (Importance/5)*ImportanceWeight + (Demand/5)*DemandWeight +
/// ((5-Mastery)/4)*MasteryGapWeight, or PriorityOverride.Score when set. Not persisted — the
/// ranked-list query (Infrastructure) calls this same policy in memory rather than re-expressing
/// the formula in SQL, so there is exactly one implementation; it is also used to compute a
/// single item's score breakdown for detail views without a second query.
/// </summary>
public static class EffectiveScorePolicy
{
    public static int Resolve(int importance, decimal demand, decimal mastery, ScoringWeights weights, PriorityOverride? priorityOverride)
    {
        return priorityOverride?.Score ?? Compute(importance, demand, mastery, weights);
    }

    public static int Compute(int importance, decimal demand, decimal mastery, ScoringWeights weights)
    {
        return ComputeBreakdown(importance, demand, mastery, weights).Total;
    }

    public static ScoreBreakdown ComputeBreakdown(int importance, decimal demand, decimal mastery, ScoringWeights weights)
    {
        var importanceContribution = importance / 5m * weights.ImportanceWeight;
        var demandContribution = demand / 5m * weights.DemandWeight;
        var masteryGapContribution = (5m - mastery) / 4m * weights.MasteryGapWeight;
        var total = (int)Math.Round(importanceContribution + demandContribution + masteryGapContribution, MidpointRounding.AwayFromZero);

        return new ScoreBreakdown(importanceContribution, demandContribution, masteryGapContribution, total);
    }
}
