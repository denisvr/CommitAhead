using System.Text.RegularExpressions;

namespace CommitAhead.Domain.StudyItems;

/// <summary>
/// Shared by StudyItem.Tags and LeetCodeDetails.Patterns — both are described in
/// docs/domain/model.md as "normalised string[]": trim, lowercase, kebab-case, deduplicated.
///
/// Allowed-character policy: only ASCII letters and digits survive as literal characters. Any
/// run of anything else — whitespace, underscores, punctuation, or a mix of them — collapses to
/// a single hyphen, and leading/trailing hyphens are dropped. "C++ Basics", "c++_basics", and
/// "  C++  Basics  " all normalise to the same "c-basics".
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
        var kebab = NonAlphanumericRun().Replace(lowered, "-");
        return kebab.Trim('-');
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphanumericRun();
}
