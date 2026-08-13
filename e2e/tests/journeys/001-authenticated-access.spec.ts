import { test, expect } from '../fixtures/e2e-test.js';

// docs/testing/strategy.md §7.1 — proves security controls are in place: an unauthenticated
// visitor is kept out, a test-issued session is consumed by the real authentication pipeline and
// authorizes the app shell + GET /api/me, and logout ends the session server-side (not just
// client-side state).
test('unauthenticated visitor is kept out; a minted session authorizes access; logout ends it', async ({ page, authenticatedPage }) => {
  await test.step('an unauthenticated visitor sees the login screen', async () => {
    await page.goto('/');
    await expect(page.getByRole('heading', { name: 'CommitAhead' })).toBeVisible();
    await expect(page.getByLabel('Email')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Send sign-in link' })).toBeVisible();
  });

  await test.step('unauthenticated access cannot expose protected application content', async () => {
    await expect(page.getByRole('heading', { name: 'Study queue' })).not.toBeVisible();

    const meResponse = await page.request.get('/api/me');
    expect(meResponse.status()).toBe(401);
  });

  await test.step('a test-issued session is consumed by the real authentication pipeline and the app shell loads', async () => {
    await authenticatedPage.goto('/');
    await expect(authenticatedPage.getByRole('heading', { name: 'Study queue' })).toBeVisible();
    await expect(authenticatedPage.getByText('e2e@commitahead.local')).toBeVisible();
  });

  await test.step('GET /api/me succeeds for the authenticated session', async () => {
    const meResponse = await authenticatedPage.request.get('/api/me');
    expect(meResponse.status()).toBe(200);
    expect(await meResponse.json()).toEqual({ email: 'e2e@commitahead.local' });
  });

  await test.step('logout ends the session and protected content is no longer accessible', async () => {
    await authenticatedPage.getByRole('button', { name: 'Log out' }).click();
    await expect(authenticatedPage.getByLabel('Email')).toBeVisible();
    await expect(authenticatedPage.getByRole('heading', { name: 'Study queue' })).not.toBeVisible();

    expect((await authenticatedPage.request.get('/api/me')).status()).toBe(401);

    // Reload re-runs the /api/me check from scratch against the browser's real cookies, proving
    // the server-side session was actually cleared rather than only the client-side auth state.
    await authenticatedPage.reload();
    await expect(authenticatedPage.getByLabel('Email')).toBeVisible();
    await expect(authenticatedPage.getByRole('heading', { name: 'Study queue' })).not.toBeVisible();
  });
});
