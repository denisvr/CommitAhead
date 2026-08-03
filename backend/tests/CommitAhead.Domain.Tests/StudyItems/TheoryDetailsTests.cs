using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Domain.Tests.StudyItems;

public class TheoryDetailsTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankSummaryMarkdown_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => new TheoryDetails(value, [], [], []));
    }

    [Fact]
    public void Constructor_WithBlankKeyPointEntry_Throws()
    {
        Assert.Throws<ArgumentException>(() => new TheoryDetails("Summary", ["   "], [], []));
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("ftp://example.com/cap")]
    public void Constructor_WithNonHttpReference_Throws(string reference)
    {
        Assert.Throws<ArgumentException>(() => new TheoryDetails("Summary", [], [], [reference]));
    }

    [Fact]
    public void Constructor_WithHttpOrHttpsReferences_Succeeds()
    {
        var details = new TheoryDetails("Summary", [], [], ["http://example.com/a", "https://example.com/b"]);

        Assert.Equal(["http://example.com/a", "https://example.com/b"], details.References);
    }
}
