using CommitAhead.Application.AI;
using CommitAhead.Domain.StudyItems;

namespace CommitAhead.Application.Tests.AI;

public class AiStudyItemDetailsParserTests
{
    [Fact]
    public void Parse_WithAnUndefinedCategory_ThrowsAiResponseValidationException()
    {
        Assert.Throws<AiResponseValidationException>(() => AiStudyItemDetailsParser.Parse((StudyItemCategory)999, "{}"));
    }

    [Fact]
    public void Parse_WithMalformedJson_ThrowsAiResponseValidationException()
    {
        Assert.Throws<AiResponseValidationException>(() => AiStudyItemDetailsParser.Parse(StudyItemCategory.Theory, "not json"));
    }

    [Fact]
    public void Parse_WithAnUnknownProperty_ThrowsAiResponseValidationException()
    {
        const string json = """{"SummaryMarkdown":"Summary","KeyPoints":["Point"],"InterviewQuestions":["Question?"],"References":["https://example.com"],"Unexpected":"value"}""";

        Assert.Throws<AiResponseValidationException>(() => AiStudyItemDetailsParser.Parse(StudyItemCategory.Theory, json));
    }

    [Fact]
    public void Parse_WithAValidTheoryPayload_ReturnsTheoryDetails()
    {
        const string json = """{"SummaryMarkdown":"Summary","KeyPoints":["Point"],"InterviewQuestions":["Question?"],"References":["https://example.com"]}""";

        var details = AiStudyItemDetailsParser.Parse(StudyItemCategory.Theory, json);

        var theoryDetails = Assert.IsType<TheoryDetails>(details);
        Assert.Equal("Summary", theoryDetails.SummaryMarkdown);
    }

    /// <summary>
    /// Regression coverage tying AnthropicStructuredOutputSchema's *DetailsSchema() PascalCase
    /// field names (Journey 3 casing fix) to what this parser actually accepts, for every category
    /// a real AnalyzeX StudyItemProposal could declare — not just Theory.
    /// </summary>
    [Fact]
    public void Parse_WithAValidLeetCodePayload_ReturnsLeetCodeDetails()
    {
        const string json =
            """{"ProblemNumber":42,"Url":"https://leetcode.com/problems/two-sum","Difficulty":"Medium","Patterns":["Hash map"],"ExpectedTimeComplexity":"O(n)","ExpectedSpaceComplexity":"O(n)","ApproachMarkdown":"Use a hash map.","CSharpSolution":null}""";

        var details = AiStudyItemDetailsParser.Parse(StudyItemCategory.LeetCode, json);

        var leetCodeDetails = Assert.IsType<LeetCodeDetails>(details);
        Assert.Equal(42, leetCodeDetails.ProblemNumber);
    }

    [Fact]
    public void Parse_WithAValidSystemDesignPayload_ReturnsSystemDesignDetails()
    {
        const string json =
            """{"PromptMarkdown":"Design a URL shortener.","ClarifyingQuestions":["Scale?"],"FunctionalRequirements":["Shorten a URL"],"NonFunctionalRequirements":["Low latency"],"EvaluationChecklist":["Discusses sharding"],"ReferenceSolutionMarkdown":"Use a hash-based short code."}""";

        var details = AiStudyItemDetailsParser.Parse(StudyItemCategory.SystemDesign, json);

        var systemDesignDetails = Assert.IsType<SystemDesignDetails>(details);
        Assert.Equal("Design a URL shortener.", systemDesignDetails.PromptMarkdown);
    }

    [Fact]
    public void Parse_WithAValidBehavioralPayload_ReturnsBehavioralDetails()
    {
        const string json =
            """{"Competencies":["Leadership"],"QuestionVariants":["Tell me about a time..."],"Situation":"A project was behind schedule.","Task":"Get it back on track.","Action":"Reprioritized scope.","Result":"Shipped on time.","Reflection":null}""";

        var details = AiStudyItemDetailsParser.Parse(StudyItemCategory.Behavioral, json);

        var behavioralDetails = Assert.IsType<BehavioralDetails>(details);
        Assert.Equal("A project was behind schedule.", behavioralDetails.Situation);
    }
}
