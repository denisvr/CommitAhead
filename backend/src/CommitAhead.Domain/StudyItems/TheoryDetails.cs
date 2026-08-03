namespace CommitAhead.Domain.StudyItems;

public sealed class TheoryDetails : StudyItemDetails
{
    public string SummaryMarkdown { get; }
    public IReadOnlyList<string> KeyPoints { get; }
    public IReadOnlyList<string> InterviewQuestions { get; }
    public IReadOnlyList<string> References { get; }

    public TheoryDetails(
        string summaryMarkdown,
        IEnumerable<string> keyPoints,
        IEnumerable<string> interviewQuestions,
        IEnumerable<string> references)
    {
        SummaryMarkdown = TextValidation.RequireNonBlank(summaryMarkdown, nameof(summaryMarkdown), ValidationLimits.MarkdownMaxLength);
        KeyPoints = TextValidation.RequireEntries(keyPoints, nameof(keyPoints));
        InterviewQuestions = TextValidation.RequireEntries(interviewQuestions, nameof(interviewQuestions));
        References = references.Select(reference => TextValidation.ValidateAbsoluteUrl(reference, nameof(references), "http", "https")).ToList();
    }
}
