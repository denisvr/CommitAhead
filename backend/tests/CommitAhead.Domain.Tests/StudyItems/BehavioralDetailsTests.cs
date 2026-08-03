using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Domain.Tests.StudyItems;

public class BehavioralDetailsTests
{
    private static BehavioralDetails CreateDetails(
        string situation = "A production outage",
        string task = "Restore service",
        string action = "Rolled back the deploy",
        string result = "Service restored in 10 minutes",
        IEnumerable<string>? competencies = null) => new(
        competencies ?? [],
        [],
        situation,
        task,
        action,
        result,
        reflection: null);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankSituation_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => CreateDetails(situation: value));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankTask_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => CreateDetails(task: value));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankAction_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => CreateDetails(action: value));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankResult_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => CreateDetails(result: value));
    }

    [Fact]
    public void Constructor_WithBlankCompetencyEntry_Throws()
    {
        Assert.Throws<ArgumentException>(() => CreateDetails(competencies: ["   "]));
    }

    [Fact]
    public void Constructor_WithoutReflection_Succeeds()
    {
        var details = CreateDetails();

        Assert.Null(details.Reflection);
    }
}
