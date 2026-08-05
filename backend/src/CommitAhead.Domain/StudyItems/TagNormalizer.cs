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
///
/// Deliberately no synonym resolution: this is a fixed transform, not a lookup table, so it has
/// no way to know "C#" and "C Sharp" name the same thing — a BARE ambiguous term (no other word
/// attached to absorb the punctuation into a hyphen) can normalise away entirely, e.g. "C#" alone
/// -> "c". docs/domain/model.md documents the spellings ("C Sharp", "C Plus Plus", "dotnet") that
/// land on the conventional tag instead.
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
        // A deserialized request body can still hand this a null element at runtime regardless of
        // the static, non-nullable `string` in IEnumerable<string> — a null entry is as meaningless
        // as an all-whitespace one, so both normalise to "" and are dropped by Normalize's filter
        // below, rather than throwing. Unlike TextValidation's list fields, a blank/absent tag was
        // never a meaningful data slot to reject in the first place.
        var lowered = (value ?? string.Empty).Trim().ToLowerInvariant();
        var kebab = NonAlphanumericRun().Replace(lowered, "-");
        return kebab.Trim('-');
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphanumericRun();
}
