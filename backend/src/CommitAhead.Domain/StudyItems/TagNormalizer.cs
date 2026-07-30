using System.Text.RegularExpressions;

namespace CommitAhead.Domain.StudyItems;

/// <summary>
/// Shared by StudyItem.Tags and LeetCodeDetails.Patterns — both are described in
/// docs/domain/model.md as "normalised string[]": trim, lowercase, kebab-case, deduplicated.
/// </summary>
internal static partial class TagNormalizer
{
    public static IReadOnlyList<string> Normalize(IEnumerable<string> values)
    {
        return values
            .Select(NormalizeOne)
            .Where(value => value.Length > 0)
            .Distinct()
            .ToList();
    }

    private static string NormalizeOne(string value)
    {
        var lowered = value.Trim().ToLowerInvariant();
        var withHyphens = WhitespaceRun().Replace(lowered, "-");
        var collapsed = HyphenRun().Replace(withHyphens, "-");
        return collapsed.Trim('-');
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRun();

    [GeneratedRegex("-+")]
    private static partial Regex HyphenRun();
}
