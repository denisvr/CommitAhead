using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Domain.Tests.StudyItems;

public class EffectiveScorePolicyTests
{
    [Fact]
    public void Compute_AtMinimumInputs_Returns8()
    {
        // ADR-0003: importance=1, demand=0, mastery=5 -> the documented minimum computed score.
        var score = EffectiveScorePolicy.Compute(importance: 1, demand: 0m, mastery: 5m, ScoringWeights.Default);

        Assert.Equal(8, score);
    }

    [Fact]
    public void Compute_AtMaximumInputs_Returns100()
    {
        var score = EffectiveScorePolicy.Compute(importance: 5, demand: 5m, mastery: 1m, ScoringWeights.Default);

        Assert.Equal(100, score);
    }

    [Fact]
    public void Compute_WithMidpointInputs_MatchesFormula()
    {
        // importance=3, demand=2.5, mastery=3, default weights 40/35/25:
        // (3/5)*40 + (2.5/5)*35 + ((5-3)/4)*25 = 24 + 17.5 + 12.5 = 54
        var score = EffectiveScorePolicy.Compute(importance: 3, demand: 2.5m, mastery: 3m, ScoringWeights.Default);

        Assert.Equal(54, score);
    }

    [Fact]
    public void Resolve_WithoutPriorityOverride_ReturnsComputedScore()
    {
        var resolved = EffectiveScorePolicy.Resolve(importance: 1, demand: 0m, mastery: 5m, ScoringWeights.Default, priorityOverride: null);

        Assert.Equal(8, resolved);
    }

    [Fact]
    public void Resolve_WithPriorityOverride_ReturnsOverrideScore_IgnoringInputs()
    {
        var priorityOverride = new PriorityOverride(0, "Deprioritised after offer accepted");

        var resolved = EffectiveScorePolicy.Resolve(importance: 5, demand: 5m, mastery: 1m, ScoringWeights.Default, priorityOverride);

        Assert.Equal(0, resolved);
    }

    [Fact]
    public void Compute_RoundsToNearestInteger()
    {
        // importance=1, demand=1, mastery=3, default weights:
        // (1/5)*40 + (1/5)*35 + ((5-3)/4)*25 = 8 + 7 + 12.5 = 27.5 -> rounds away from zero to 28
        var score = EffectiveScorePolicy.Compute(importance: 1, demand: 1m, mastery: 3m, ScoringWeights.Default);

        Assert.Equal(28, score);
    }

    [Fact]
    public void ComputeBreakdown_ReturnsTheThreeWeightedTermsAndTheirRoundedTotal()
    {
        // Same inputs as Compute_WithMidpointInputs_MatchesFormula: 24 + 17.5 + 12.5 = 54.
        var breakdown = EffectiveScorePolicy.ComputeBreakdown(importance: 3, demand: 2.5m, mastery: 3m, ScoringWeights.Default);

        Assert.Equal(24m, breakdown.ImportanceContribution);
        Assert.Equal(17.5m, breakdown.DemandContribution);
        Assert.Equal(12.5m, breakdown.MasteryGapContribution);
        Assert.Equal(54, breakdown.Total);
    }

    [Fact]
    public void ComputeBreakdown_TotalMatchesCompute()
    {
        var breakdown = EffectiveScorePolicy.ComputeBreakdown(importance: 1, demand: 1m, mastery: 3m, ScoringWeights.Default);
        var computed = EffectiveScorePolicy.Compute(importance: 1, demand: 1m, mastery: 3m, ScoringWeights.Default);

        Assert.Equal(computed, breakdown.Total);
    }
}
