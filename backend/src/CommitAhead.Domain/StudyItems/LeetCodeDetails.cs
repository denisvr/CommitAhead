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

        if (!Enum.IsDefined(difficulty))
        {
            throw new ArgumentOutOfRangeException(nameof(difficulty));
        }

        ProblemNumber = problemNumber;
        Url = TextValidation.ValidateOptionalAbsoluteUrl(url, nameof(url), "https");
        Difficulty = difficulty;
        Patterns = TagNormalizer.Normalize(patterns);
        ExpectedTimeComplexity = TextValidation.RequireNonBlank(expectedTimeComplexity, nameof(expectedTimeComplexity), ValidationLimits.ShortTextMaxLength);
        ExpectedSpaceComplexity = TextValidation.RequireNonBlank(expectedSpaceComplexity, nameof(expectedSpaceComplexity), ValidationLimits.ShortTextMaxLength);
        ApproachMarkdown = TextValidation.RequireNonBlank(approachMarkdown, nameof(approachMarkdown), ValidationLimits.MarkdownMaxLength);
        CSharpSolution = TextValidation.TrimToNullOrValidate(csharpSolution, nameof(csharpSolution), ValidationLimits.MarkdownMaxLength);
    }
}
