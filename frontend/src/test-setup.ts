import '@testing-library/jest-dom/vitest'
import { afterEach } from 'vitest'
import { cleanup } from '@testing-library/react'

// vitest.config.ts doesn't set test.globals, so @testing-library/react's own auto-cleanup
// detection (which looks for a global afterEach) never fires — without this, every render stays
// mounted into the next test's document, and multi-render/async tests start finding duplicate
// elements left over from previous tests.
afterEach(() => {
  cleanup()
})
