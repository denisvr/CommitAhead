using CommitAhead.Domain;

namespace CommitAhead.Domain.JobAnalyses;

/// <summary>
/// Shared string invariant checks for JobAnalysis and its value objects/children. A local copy,
/// not shared with other aggregates' <c>TextValidation</c> — same precedent as
/// <see cref="ValidationLimits"/>. Never truncates — every violation throws
/// <see cref="DomainValidationException"/>.
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
}
