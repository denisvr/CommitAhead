using CommitAhead.Domain;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Domain.Tests.StudyItems;

public class SystemDesignDetailsTests
{
    private static SystemDesignDetails CreateDetails(
        string promptMarkdown = "Design a URL shortener",
        IEnumerable<string>? clarifyingQuestions = null,
        string referenceSolutionMarkdown = "Reference solution") => new(
        promptMarkdown,
        clarifyingQuestions ?? [],
        [],
        [],
        [],
        referenceSolutionMarkdown);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankPromptMarkdown_Throws(string value)
    {
        Assert.Throws<DomainValidationException>(() => CreateDetails(promptMarkdown: value));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankReferenceSolutionMarkdown_Throws(string value)
    {
        Assert.Throws<DomainValidationException>(() => CreateDetails(referenceSolutionMarkdown: value));
    }

    [Fact]
    public void Constructor_WithBlankClarifyingQuestionEntry_Throws()
    {
        Assert.Throws<DomainValidationException>(() => CreateDetails(clarifyingQuestions: ["   "]));
    }

    [Fact]
    public void Constructor_WithValidArguments_Succeeds()
    {
        var details = CreateDetails();

        Assert.Equal("Design a URL shortener", details.PromptMarkdown);
        Assert.Equal("Reference solution", details.ReferenceSolutionMarkdown);
    }
}
