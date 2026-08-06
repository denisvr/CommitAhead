using CommitAhead.Domain;

namespace CommitAhead.Domain.InterviewNotes;

/// <summary>Shared string/list invariant checks for InterviewNote. A local copy, not shared with any other aggregate's <c>TextValidation</c>. Never truncates — every violation throws <see cref="DomainValidationException"/>.</summary>
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

    /// <summary>
    /// A deserialized request body can hand a domain constructor/method an integer that has no
    /// matching enum member (a plain C# cast never fails at that boundary) — reject it explicitly
    /// rather than silently storing an undefined value.
    /// </summary>
    public static TEnum ValidateDefined<TEnum>(TEnum value, string paramName) where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new DomainValidationException($"{paramName} is not a recognized {typeof(TEnum).Name}.");
        }

        return value;
    }

    /// <summary>
    /// Rejects a null list argument outright (not just null/blank individual entries) — unlike
    /// <c>CommitAhead.Domain.StudyItems.TextValidation.RequireEntries</c>, which would throw an
    /// unhelpful NullReferenceException from calling <c>.ToList()</c> on a null enumerable instead
    /// of a clean DomainValidationException.
    /// </summary>
    public static IReadOnlyList<string> RequireEntries(IEnumerable<string> values, string paramName)
    {
        if (values is null)
        {
            throw new DomainValidationException($"{paramName} must not be null.");
        }

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
}
