using CommitAhead.Domain;

namespace CommitAhead.Domain.StudyItems;

/// <summary>
/// Shared string/list/URL invariant checks for StudyItem and its typed Details, so the same rule
/// (required-and-trimmed, max length, no blank/null list entries, absolute-URL-with-scheme) isn't
/// reimplemented slightly differently per field. Never truncates — every violation throws
/// DomainValidationException.
/// </summary>
internal static class TextValidation
{
    public static string RequireNonBlank(string value, string paramName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"{paramName} is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainValidationException($"{paramName} must be at most {maxLength} characters.");
        }

        return trimmed;
    }

    public static string? TrimToNullOrValidate(string? value, string paramName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainValidationException($"{paramName} must be at most {maxLength} characters.");
        }

        return trimmed;
    }

    public static IReadOnlyList<string> RequireEntries(IEnumerable<string> values, string paramName)
    {
        var list = values.ToList();
        if (list.Count > ValidationLimits.MaxListEntryCount)
        {
            throw new DomainValidationException($"{paramName} must have at most {ValidationLimits.MaxListEntryCount} entries.");
        }

        var result = new List<string>(list.Count);
        foreach (var value in list)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new DomainValidationException($"{paramName} entries must not be null or blank.");
            }

            var trimmed = value.Trim();
            if (trimmed.Length > ValidationLimits.ListEntryMaxLength)
            {
                throw new DomainValidationException($"{paramName} entries must be at most {ValidationLimits.ListEntryMaxLength} characters.");
            }

            result.Add(trimmed);
        }

        return result;
    }

    public static string? ValidateOptionalAbsoluteUrl(string? value, string paramName, params string[] allowedSchemes)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return ValidateAbsoluteUrl(value, paramName, allowedSchemes);
    }

    public static string ValidateAbsoluteUrl(string value, string paramName, params string[] allowedSchemes)
    {
        if (value.Length > ValidationLimits.UrlMaxLength
            || !Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || !allowedSchemes.Contains(uri.Scheme, StringComparer.Ordinal))
        {
            throw new DomainValidationException($"{paramName} must be an absolute URL using {string.Join(" or ", allowedSchemes)}.");
        }

        return value;
    }
}
