import { toNumber, type YearMonthDto } from './api'

const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']

export function formatMonthYear(value: YearMonthDto): string {
  const month = toNumber(value.month)
  const year = toNumber(value.year)
  return `${MONTHS[Math.max(0, Math.min(11, month - 1))]} ${year}`
}

function monthIndex(value: YearMonthDto): number {
  return toNumber(value.year) * 12 + toNumber(value.month)
}

export function formatDateRange(start: YearMonthDto, end: YearMonthDto | null): string {
  return `${formatMonthYear(start)} — ${end ? formatMonthYear(end) : 'Present'}`
}

export function formatDuration(start: YearMonthDto, end: YearMonthDto | null): string {
  const endIndex = end ? monthIndex(end) : monthIndex(currentYearMonth())
  const months = Math.max(1, endIndex - monthIndex(start) + 1)
  const years = Math.floor(months / 12)
  const remainder = months % 12
  const parts: string[] = []
  if (years > 0) parts.push(`${years} yr${years === 1 ? '' : 's'}`)
  if (remainder > 0 || years === 0) parts.push(`${remainder} mo`)
  return parts.join(' ')
}

// Not Date.now() directly — kept as one seam so a future "as of" override is a one-line change.
function currentYearMonth(): YearMonthDto {
  const now = new Date()
  return { year: now.getFullYear(), month: now.getMonth() + 1 }
}

export function compareStartDateDesc(a: { startDate: YearMonthDto }, b: { startDate: YearMonthDto }): number {
  return monthIndex(b.startDate) - monthIndex(a.startDate)
}

export function totalDuration(entries: { startDate: YearMonthDto; endDate: YearMonthDto | null }[]): string {
  if (entries.length === 0) return '0 mo'
  const totalMonths = entries.reduce((sum, entry) => {
    const endIndex = entry.endDate ? monthIndex(entry.endDate) : monthIndex(currentYearMonth())
    return sum + Math.max(1, endIndex - monthIndex(entry.startDate) + 1)
  }, 0)
  const years = Math.floor(totalMonths / 12)
  const remainder = totalMonths % 12
  const parts: string[] = []
  if (years > 0) parts.push(`${years} yr${years === 1 ? '' : 's'}`)
  if (remainder > 0 || years === 0) parts.push(`${remainder} mo`)
  return parts.join(' ')
}
