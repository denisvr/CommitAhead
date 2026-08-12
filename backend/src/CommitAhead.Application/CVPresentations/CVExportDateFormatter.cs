using System.Globalization;
using CommitAhead.Domain.ProfessionalProfiles;

namespace CommitAhead.Application.CVPresentations;

/// <summary>
/// Backend counterpart of the frontend's formatYearMonth.ts, for PDF export — same locale-aware
/// "short month + year" format (e.g. "Aug 2026"), same fail-soft posture. CVPresentation.Locale is
/// already validated at write time against CultureInfo's own culture list, but YearMonth.Year has
/// no domain bounds check, so a DateTime construction can still throw here — falls back to a plain
/// "yyyy-MM" string exactly like the frontend does on an Intl.DateTimeFormat failure.
/// </summary>
internal static class CVExportDateFormatter
{
    public static string FormatYearMonth(YearMonth? value, string locale)
    {
        if (value is null)
        {
            return string.Empty;
        }

        try
        {
            var culture = CultureInfo.GetCultureInfo(locale);
            var date = new DateTime(value.Year, value.Month, 1);
            return date.ToString("MMM yyyy", culture);
        }
        catch (Exception ex) when (ex is CultureNotFoundException or ArgumentOutOfRangeException)
        {
            return $"{value.Year:D4}-{value.Month:D2}";
        }
    }

    /// <summary>"Present" for a null end date (still ongoing) — matches the CV convention every template below relies on.</summary>
    public static string FormatDateRange(YearMonth? start, YearMonth? end, string locale)
    {
        var startText = FormatYearMonth(start, locale);
        var endText = end is null ? "Present" : FormatYearMonth(end, locale);

        return startText.Length == 0 ? endText : $"{startText} – {endText}";
    }
}
