using CommitAhead.Domain.ProfessionalProfiles;

namespace CommitAhead.Infrastructure.ProfessionalProfiles;

/// <summary>
/// EF Core cannot constructor-bind a nested owned/complex value object as a containing entity's
/// own constructor parameter ("navigations to related entities, including references to owned
/// types, cannot be bound") — every entity here has a constructor-only YearMonth field, so each
/// is mapped as a single converted int column instead (year * 100 + month) rather than the two
/// separate columns persistence.md originally described.
/// </summary>
internal static class YearMonthConversion
{
    public static int ToInt(YearMonth yearMonth) => (yearMonth.Year * 100) + yearMonth.Month;

    public static YearMonth FromInt(int value) => new(value / 100, value % 100);

    public static int? ToNullableInt(YearMonth? yearMonth) => yearMonth is null ? null : ToInt(yearMonth);

    public static YearMonth? FromNullableInt(int? value) => value is null ? null : FromInt(value.Value);
}
