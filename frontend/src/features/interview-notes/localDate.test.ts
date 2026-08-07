import { describe, it, expect, vi } from 'vitest'
import { toLocalDateInputValue } from './localDate'

describe('toLocalDateInputValue', () => {
  it('uses the local calendar date, not the UTC one', () => {
    // vi.stubEnv sets process.env.TZ without this file needing Node's ambient types — src/ is
    // browser-only (tsconfig.app.json has no "node" types), even for this Node-only test case.
    vi.stubEnv('TZ', 'Pacific/Kiritimati') // UTC+14 — always ahead of UTC
    try {
      // 23:30 UTC on Jan 14 is already Jan 15 in this UTC+14 zone.
      const date = new Date(Date.UTC(2026, 0, 14, 23, 30))
      expect(toLocalDateInputValue(date)).toBe('2026-01-15')
      expect(date.toISOString().slice(0, 10)).toBe('2026-01-14')
    } finally {
      vi.unstubAllEnvs()
    }
  })
})
