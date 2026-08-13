import { test as base, expect } from '@playwright/test';
import type { Cookie, Page } from '@playwright/test';
import { resetDatabase } from '../../scripts/reset-db.mjs';

// The single extended `test` every journey imports (docs/testing/strategy.md §7.11, §7.3, §7.4).
// No setup project, no storageState file — Playwright's built-in `page` fixture is never
// overridden and stays the anonymous page; `e2eSession`/`authenticatedPage` are lazy, so only
// tests that actually request authentication pay for it. Dependency order is
// resetDb -> e2eSession -> authenticatedPage, expressed as real fixture dependencies (destructured
// parameters), not as hooks that happen to run in a convenient order.
type Fixtures = {
  resetDb: void;
  e2eSession: Cookie[];
  authenticatedPage: Page;
};

export const test = base.extend<Fixtures>({
  // Automatic and test-scoped: every test gets a freshly reset database, whether or not it asks
  // for authentication. resetDatabase() is the only executable reset path — this fixture never
  // re-implements it.
  resetDb: [
    async ({}, use) => {
      await resetDatabase();
      await use();
    },
    { auto: true },
  ],

  // Depends on resetDb, so the E2E user row is guaranteed to exist (freshly seeded) before a
  // session is minted against it.
  e2eSession: async ({ resetDb, request }, use) => {
    const response = await request.post('/auth/e2e/session');
    if (response.status() !== 204) {
      throw new Error(
        `/auth/e2e/session returned ${response.status()} — is ASPNETCORE_ENVIRONMENT=E2E set on the app container?`,
      );
    }

    const state = await request.storageState();
    await use(state.cookies);
  },

  // A separate authenticated BrowserContext/Page built from the minted session. The built-in
  // `page` fixture is never touched, so journey 001's unauthenticated half needs no special
  // handling — it simply does not request this fixture.
  authenticatedPage: async ({ browser, e2eSession }, use) => {
    const context = await browser.newContext();
    await context.addCookies(e2eSession);
    const page = await context.newPage();
    await use(page);
    await context.close();
  },
});

export { expect };
