import { defineConfig, devices } from '@playwright/test';

// Playwright EXECUTION CONFIGURATION ONLY (docs/testing/strategy.md §7.11) — no stack lifecycle
// (no `webServer`), no auth, no seeding, no reset. Those live in scripts/run-full.mjs,
// tests/fixtures/e2e-test.ts, and scripts/reset-db.mjs respectively.
const APPROVED_BASE_URL = 'http://localhost:8081';
const baseURL = process.env.E2E_BASE_URL ?? APPROVED_BASE_URL;

// Fail fast rather than merely discourage: a wrong-target run must be impossible, not just
// documented against. This must never resolve to :8080 (the local-production stack, ADR-0021) or
// anything else.
if (baseURL !== APPROVED_BASE_URL) {
  throw new Error(
    `E2E baseURL must be exactly '${APPROVED_BASE_URL}' (got '${baseURL}') — this suite must never target the dev or local-production stacks.`,
  );
}

export default defineConfig({
  testDir: './tests/journeys',

  // workers: 1, never fullyParallel — all four journeys share one database, one seeded owner, and
  // one truncate-based reset (docs/testing/strategy.md §7.7). This bounds concurrency only; it is
  // not, and must never be relied on as, an execution-order guarantee.
  workers: 1,
  fullyParallel: false,

  retries: process.env.CI ? 1 : 0,
  forbidOnly: !!process.env.CI,

  reporter: 'html',

  use: {
    baseURL,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  // No `webServer`: the stack's lifecycle is owned by scripts/run-full.mjs / npm run stack:up,
  // never by Playwright itself — see docs/testing/strategy.md §7.2 for why.
});
