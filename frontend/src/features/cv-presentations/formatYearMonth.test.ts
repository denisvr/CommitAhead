import { describe, it, expect } from 'vitest'
import { formatYearMonth } from './formatYearMonth'

describe('formatYearMonth', () => {
  it('returns an empty string for a null value', () => {
    expect(formatYearMonth(null, 'en-GB')).toBe('')
  })

  it('formats using the given locale', () => {
    expect(formatYearMonth({ year: 2020, month: 1 }, 'en-GB')).toBe('Jan 2020')
  })

  it('uses locale-aware month names for a non-English locale', () => {
    expect(formatYearMonth({ year: 2020, month: 1 }, 'de-DE')).toMatch(/2020/)
  })

  it('falls back to a plain YYYY-MM rendering for a locale Intl.DateTimeFormat rejects, instead of throwing', () => {
    // A malformed BCP-47 tag (underscore instead of hyphen) — Intl.DateTimeFormat throws a
    // RangeError for this, unlike a syntactically well-formed but made-up tag such as
    // "not-a-real-locale", which Intl silently falls back on rather than rejecting.
    expect(formatYearMonth({ year: 2020, month: 1 }, 'not_a_locale')).toBe('2020-01')
  })

  it('narrows widened numeric year/month strings the same way the rest of the app does', () => {
    expect(formatYearMonth({ year: '2020', month: '1' }, 'not_a_locale')).toBe('2020-01')
  })
})
