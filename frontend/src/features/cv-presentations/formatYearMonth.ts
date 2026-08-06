import { toNumber, type YearMonthDto } from '../professional-profile/api'

// Real locale-aware month names (e.g. German abbreviations for de-DE) via Intl — the substantive
// part of "formatting rules." The presentation's free-text dateFormat pattern (e.g. "dd MMM
// yyyy") is not parsed/applied literally: YearMonth has no day component, and a general
// date-pattern engine for a two-field value is disproportionate to this slice.
export function formatYearMonth(value: YearMonthDto | null, locale: string): string {
  if (!value) {
    return ''
  }

  const date = new Date(toNumber(value.year), toNumber(value.month) - 1, 1)
  return new Intl.DateTimeFormat(locale, { year: 'numeric', month: 'short' }).format(date)
}
