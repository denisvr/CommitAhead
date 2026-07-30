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
        SummaryMarkdown = summaryMarkdown;
        KeyPoints = keyPoints.ToList();
        InterviewQuestions = interviewQuestions.ToList();
        References = references.ToList();
    }
}
