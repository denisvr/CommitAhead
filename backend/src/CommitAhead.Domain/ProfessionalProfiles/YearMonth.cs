using CommitAhead.Domain;

namespace CommitAhead.Domain.ProfessionalProfiles;

/// <summary>A calendar month, used wherever ProfessionalProfile entries need a date without a day (model.md).</summary>
public sealed record YearMonth : IComparable<YearMonth>
{
    public int Year { get; }
    public int Month { get; }

    public YearMonth(int year, int month)
    {
        if (month is < 1 or > 12)
        {
            throw new DomainValidationException("Month must be in [1,12].");
        }

        Year = year;
        Month = month;
    }

    public int CompareTo(YearMonth? other)
    {
        if (other is null)
        {
            return 1;
        }

        var yearComparison = Year.CompareTo(other.Year);
        return yearComparison != 0 ? yearComparison : Month.CompareTo(other.Month);
    }

    public static bool operator <(YearMonth left, YearMonth right) => left.CompareTo(right) < 0;

    public static bool operator >(YearMonth left, YearMonth right) => left.CompareTo(right) > 0;

    public static bool operator <=(YearMonth left, YearMonth right) => left.CompareTo(right) <= 0;

    public static bool operator >=(YearMonth left, YearMonth right) => left.CompareTo(right) >= 0;
}
