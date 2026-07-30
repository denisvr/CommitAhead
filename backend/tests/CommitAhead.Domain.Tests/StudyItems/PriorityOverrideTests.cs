using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Domain.Tests.StudyItems;

public class PriorityOverrideTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void Constructor_WithBoundaryScores_Succeeds(int score)
    {
        var priorityOverride = new PriorityOverride(score, "Interview next week");

        Assert.Equal(score, priorityOverride.Score);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Constructor_WithScoreOutOfRange_Throws(int score)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PriorityOverride(score, "reason"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithoutReason_Throws(string? reason)
    {
        Assert.Throws<ArgumentException>(() => new PriorityOverride(50, reason!));
    }

    [Fact]
    public void Constructor_TrimsReason()
    {
        var priorityOverride = new PriorityOverride(50, "  Interview next week  ");

        Assert.Equal("Interview next week", priorityOverride.Reason);
    }
}
