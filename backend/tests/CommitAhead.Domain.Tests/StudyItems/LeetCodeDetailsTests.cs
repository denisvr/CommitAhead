using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Domain.Tests.StudyItems;

public class LeetCodeDetailsTests
{
    [Fact]
    public void Constructor_NormalizesPatterns()
    {
        var details = new LeetCodeDetails(
            56, "https://leetcode.com/problems/merge-intervals", Difficulty.Medium,
            ["Interval Merge", "  Sorting  ", "sorting"], "O(n log n)", "O(n)", "Sort then merge", null);

        Assert.Equal(["interval-merge", "sorting"], details.Patterns);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithNonPositiveProblemNumber_Throws(int problemNumber)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LeetCodeDetails(
            problemNumber, null, Difficulty.Easy, [], "O(n)", "O(1)", "approach", null));
    }

    [Fact]
    public void Constructor_WithoutProblemNumber_Succeeds()
    {
        var details = new LeetCodeDetails(null, null, Difficulty.Easy, [], "O(n)", "O(1)", "approach", null);

        Assert.Null(details.ProblemNumber);
    }
}
