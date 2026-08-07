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
}
