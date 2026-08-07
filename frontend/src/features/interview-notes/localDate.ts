// new Date().toISOString() reads the UTC date, not the browser's own calendar date — someone west
// of UTC could see tomorrow's date default in on an evening interview note. getFullYear/Month/Date
// read the local calendar date instead.
export function toLocalDateInputValue(date: Date): string {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}
