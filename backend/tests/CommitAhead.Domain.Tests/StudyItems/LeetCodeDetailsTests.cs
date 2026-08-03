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

    [Theory]
    [InlineData("http://leetcode.com/problems/two-sum")]
    [InlineData("not a url")]
    [InlineData("javascript:alert(1)")]
    public void Constructor_WithNonHttpsUrl_Throws(string url)
    {
        Assert.Throws<ArgumentException>(() => new LeetCodeDetails(1, url, Difficulty.Easy, [], "O(n)", "O(1)", "approach", null));
    }

    [Fact]
    public void Constructor_WithHttpsUrl_Succeeds()
    {
        var details = new LeetCodeDetails(1, "https://leetcode.com/problems/two-sum", Difficulty.Easy, [], "O(n)", "O(1)", "approach", null);

        Assert.Equal("https://leetcode.com/problems/two-sum", details.Url);
    }

    [Fact]
    public void Constructor_WithUndefinedDifficulty_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LeetCodeDetails(1, null, (Difficulty)999, [], "O(n)", "O(1)", "approach", null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankExpectedTimeComplexity_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => new LeetCodeDetails(1, null, Difficulty.Easy, [], value, "O(1)", "approach", null));
    }

    [Fact]
    public void Constructor_WithBlankApproachMarkdown_Throws()
    {
        Assert.Throws<ArgumentException>(() => new LeetCodeDetails(1, null, Difficulty.Easy, [], "O(n)", "O(1)", "   ", null));
    }
}
