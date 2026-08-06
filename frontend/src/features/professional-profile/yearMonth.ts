import { toNumber, type YearMonthDto } from './api'

// <input type="month"> speaks "YYYY-MM" natively — no custom two-spinner control needed for a
// value that only ever carries a year and a month.
export function toMonthInputValue(value: YearMonthDto | null): string {
  if (!value) {
    return ''
  }

  return `${toNumber(value.year).toString().padStart(4, '0')}-${toNumber(value.month).toString().padStart(2, '0')}`
}

export function fromMonthInputValue(value: string): YearMonthDto | null {
  if (!value) {
    return null
  }

  const [year, month] = value.split('-').map(Number)
  return { year, month }
}
