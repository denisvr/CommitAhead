namespace CommitAhead.Domain.StudyItems;

public sealed class LeetCodeDetails : StudyItemDetails
{
    public int? ProblemNumber { get; }
    public string? Url { get; }
    public Difficulty Difficulty { get; }
    public IReadOnlyList<string> Patterns { get; }
    public string ExpectedTimeComplexity { get; }
    public string ExpectedSpaceComplexity { get; }
    public string ApproachMarkdown { get; }
    public string? CSharpSolution { get; }

    public LeetCodeDetails(
        int? problemNumber,
        string? url,
        Difficulty difficulty,
        IEnumerable<string> patterns,
        string expectedTimeComplexity,
        string expectedSpaceComplexity,
        string approachMarkdown,
        string? csharpSolution)
    {
        if (problemNumber is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(problemNumber), "ProblemNumber must be positive when provided.");
        }

        ProblemNumber = problemNumber;
        Url = url;
        Difficulty = difficulty;
        Patterns = TagNormalizer.Normalize(patterns);
        ExpectedTimeComplexity = expectedTimeComplexity;
        ExpectedSpaceComplexity = expectedSpaceComplexity;
        ApproachMarkdown = approachMarkdown;
        CSharpSolution = csharpSolution;
    }
}
