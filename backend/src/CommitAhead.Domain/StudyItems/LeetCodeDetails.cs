using CommitAhead.Domain;

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
            throw new DomainValidationException("ProblemNumber must be positive when provided.");
        }

        if (!Enum.IsDefined(difficulty))
        {
            throw new DomainValidationException("Difficulty is not a recognized value.");
        }

        ProblemNumber = problemNumber;
        Url = TextValidation.ValidateOptionalAbsoluteUrl(url, nameof(url), "https");
        Difficulty = difficulty;
        Patterns = ValidatePatterns(patterns);
        ExpectedTimeComplexity = TextValidation.RequireNonBlank(expectedTimeComplexity, nameof(expectedTimeComplexity), ValidationLimits.ShortTextMaxLength);
        ExpectedSpaceComplexity = TextValidation.RequireNonBlank(expectedSpaceComplexity, nameof(expectedSpaceComplexity), ValidationLimits.ShortTextMaxLength);
        ApproachMarkdown = TextValidation.RequireNonBlank(approachMarkdown, nameof(approachMarkdown), ValidationLimits.MarkdownMaxLength);
        CSharpSolution = TextValidation.TrimToNullOrValidate(csharpSolution, nameof(csharpSolution), ValidationLimits.MarkdownMaxLength);
    }

    // Same count/length ceiling as StudyItem.Tags (ValidateTags) — Patterns is the same kind of
    // normalized tag-like list, just scoped to one LeetCode problem's details instead of the item
    // as a whole.
    private static IReadOnlyList<string> ValidatePatterns(IEnumerable<string> patterns)
    {
        var normalized = TagNormalizer.Normalize(patterns);
        if (normalized.Count > ValidationLimits.MaxTagCount)
        {
            throw new DomainValidationException($"Patterns must have at most {ValidationLimits.MaxTagCount} entries.");
        }

        if (normalized.Any(pattern => pattern.Length > ValidationLimits.TagMaxLength))
        {
            throw new DomainValidationException($"Each pattern must be at most {ValidationLimits.TagMaxLength} characters.");
        }

        return normalized;
    }
}
