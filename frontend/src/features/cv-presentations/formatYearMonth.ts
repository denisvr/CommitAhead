import { toNumber, type YearMonthDto } from '../professional-profile/api'

// Real locale-aware month names (e.g. German abbreviations for de-DE) via Intl — the substantive
// part of "formatting rules." The presentation's free-text dateFormat pattern (e.g. "dd MMM
// yyyy") is not parsed/applied literally: YearMonth has no day component, and a general
// date-pattern engine for a two-field value is disproportionate to this slice.
//
// The backend now rejects an unrecognized locale on write (CVPresentation.ValidateLocale), but
// this still has to tolerate already-persisted data from before that guard existed, or a locale
// .NET's culture list accepts that Intl doesn't recognize — Intl.DateTimeFormat throws a
// RangeError for an unrecognized tag, which would otherwise crash the whole preview over one bad
// field. Falls back to a plain "YYYY-MM" rendering, which never throws.
export function formatYearMonth(value: YearMonthDto | null, locale: string): string {
  if (!value) {
    return ''
  }

  const year = toNumber(value.year)
  const month = toNumber(value.month)
  const date = new Date(year, month - 1, 1)

  try {
    return new Intl.DateTimeFormat(locale, { year: 'numeric', month: 'short' }).format(date)
  } catch {
    return `${year.toString().padStart(4, '0')}-${month.toString().padStart(2, '0')}`
  }
}
