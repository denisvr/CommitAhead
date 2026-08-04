import '@testing-library/jest-dom/vitest'
import { afterAll, afterEach } from 'vitest'
import { cleanup } from '@testing-library/react'
import { server } from './mocks/server'

// vitest.config.ts doesn't set test.globals, so @testing-library/react's own auto-cleanup
// detection (which looks for a global afterEach) never fires — without this, every render stays
// mounted into the next test's document, and multi-render/async tests start finding duplicate
// elements left over from previous tests.
afterEach(() => {
  cleanup()
})

// docs/testing/strategy.md: Vitest + RTL + MSW. Called at module scope, not inside beforeAll —
// api/client.ts's openapi-fetch createClient() captures globalThis.fetch once, at module-load
// time, into a closure; a hook only runs once every test file has already been imported, by which
// point that capture already happened against the real, unpatched fetch. Setup files run and
// complete before the test file (and its imports) load, so listening here patches fetch first.
server.listen({ onUnhandledRequest: 'error' })
afterEach(() => server.resetHandlers())
afterAll(() => server.close())
