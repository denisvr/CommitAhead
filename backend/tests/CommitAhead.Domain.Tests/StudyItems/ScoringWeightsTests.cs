using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Domain.Tests.StudyItems;

public class ScoringWeightsTests
{
    [Fact]
    public void Default_Is40_35_25()
    {
        var defaults = ScoringWeights.Default;

        Assert.Equal(40, defaults.ImportanceWeight);
        Assert.Equal(35, defaults.DemandWeight);
        Assert.Equal(25, defaults.MasteryGapWeight);
    }

    [Fact]
    public void Constructor_WhenWeightsDoNotSumTo100_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ScoringWeights(40, 35, 20));
    }

    [Theory]
    [InlineData(-1, 50, 51)]
    [InlineData(50, -1, 51)]
    [InlineData(50, 51, -1)]
    public void Constructor_WithNegativeWeight_Throws(int importance, int demand, int masteryGap)
    {
        Assert.Throws<ArgumentException>(() => new ScoringWeights(importance, demand, masteryGap));
    }

    [Fact]
    public void Constructor_WithValidWeights_Succeeds()
    {
        var weights = new ScoringWeights(50, 30, 20);

        Assert.Equal(50, weights.ImportanceWeight);
        Assert.Equal(30, weights.DemandWeight);
        Assert.Equal(20, weights.MasteryGapWeight);
    }
}
