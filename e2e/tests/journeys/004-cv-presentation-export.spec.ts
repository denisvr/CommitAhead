import { randomUUID } from 'node:crypto';
import { test, expect } from '../fixtures/e2e-test.js';

// docs/testing/strategy.md §7.1 — proves at least one CVPresentation can be edited and exported.
// The ProfessionalProfile + one Experience entry are seeded via API (§7.9: legitimate setup, not
// the behavior under test — "seeding a ProfessionalProfile with canonical entries so journey 4 has
// something to select"). The CVPresentation itself, adding that entry to its selections, and the
// export/download are all driven through the UI, which is the behavior this journey proves.
test('editing a CVPresentation\'s selections and exporting downloads a real PDF', async ({ authenticatedPage: page }) => {
  const experienceRole = 'Staff Engineer';
  const experienceCompany = 'Acme Corp';
  const presentationLabel = 'UK — Senior Backend Engineer';

  await test.step('a ProfessionalProfile with one Experience entry is seeded via API', async () => {
    const csrfResponse = await page.request.get('/auth/csrf');
    expect(csrfResponse.status()).toBe(200);
    const { token } = await csrfResponse.json();

    const profileResponse = await page.request.post('/api/professional-profile', {
      headers: { 'X-CSRF-TOKEN': token },
      data: {
        contactInfo: { name: 'Jordan Rivera', email: 'jordan.rivera@example.com', phone: null, address: null, photoStorageKey: null },
        summaryMarkdown: 'Backend engineer focused on distributed systems.',
      },
    });
    expect(profileResponse.status()).toBe(201);

    const experienceResponse = await page.request.put('/api/professional-profile/experience', {
      headers: { 'X-CSRF-TOKEN': token },
      data: [
        {
          id: randomUUID(),
          company: experienceCompany,
          client: null,
          role: experienceRole,
          employmentType: 'Permanent',
          startDate: { year: 2020, month: 1 },
          endDate: null,
          location: null,
          workMode: 'Remote',
          summaryMarkdown: 'Led backend platform initiatives.',
          achievements: [],
          skillIds: [],
        },
      ],
    });
    expect(experienceResponse.status()).toBe(204);
  });

  await test.step('the Profile Preview dialog stays hidden while closed, and opens/closes correctly below 1280px', async () => {
    // Regression coverage: `.previewDialog` once set `display: flex` unconditionally, which in
    // Chromium overrides the browser's own `dialog:not([open]) { display: none }` — a *closed*
    // dialog stayed visible. Only a real browser's UA stylesheet can prove this; jsdom-based
    // component tests don't even load the CSS.
    await page.goto('/');
    await page.getByRole('button', { name: 'Professional profile', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'Professional profile' })).toBeVisible();

    // Desktop width (this project's default viewport, 1280x720): the preview renders inline in
    // the aside column, and the dialog must not be visible while closed.
    await expect(page.locator('dialog')).toBeHidden();
    await expect(page.getByRole('button', { name: 'Preview' })).toBeHidden();

    // Below 1280px the inline preview column disappears and the one "Preview" control takes over.
    await page.setViewportSize({ width: 767, height: 900 });
    const previewButton = page.getByRole('button', { name: 'Preview' });
    await expect(previewButton).toBeVisible();

    const dialog = page.locator('dialog');
    await previewButton.click();
    await expect(dialog).toBeVisible();
    await expect(dialog.getByRole('heading', { name: 'Profile preview' })).toBeVisible();

    await page.getByRole('button', { name: 'Close preview' }).click();
    await expect(dialog).toBeHidden();

    await previewButton.click();
    await expect(dialog).toBeVisible();
    await page.keyboard.press('Escape');
    await expect(dialog).toBeHidden();

    // Back to this journey's normal desktop viewport for the remaining steps.
    await page.setViewportSize({ width: 1280, height: 720 });
  });

  await test.step('a CVPresentation is created entirely through the UI', async () => {
    await page.goto('/');
    await page.getByRole('button', { name: 'CV presentations', exact: true }).click();
    await page.getByRole('button', { name: 'New CV presentation' }).first().click();

    await page.getByLabel('Label').fill(presentationLabel);
    await page.getByLabel('Target market').fill('United Kingdom');
    await page.getByRole('button', { name: 'Create' }).click();

    await expect(page.getByRole('heading', { name: presentationLabel })).toBeVisible();
  });

  await test.step('the seeded Experience entry is added to the presentation\'s selections through the UI', async () => {
    await page.getByLabel('Add experience entry').selectOption({ label: `${experienceRole} — ${experienceCompany}` });

    // Wait for the selection save to actually complete before exporting. SelectionSection only
    // re-renders the selected list from state after its PUT resolves (no optimistic update), so
    // this entry becoming visible is itself proof the save landed — exporting before it lands
    // would export a stale, empty CV.
    await expect(page.getByText(`${experienceRole} — ${experienceCompany}`)).toBeVisible();
  });

  await test.step('exporting downloads a real PDF', async () => {
    const [download] = await Promise.all([page.waitForEvent('download'), page.getByRole('button', { name: 'Download PDF' }).click()]);

    expect(download.suggestedFilename()).toMatch(/\.pdf$/);
    expect(await download.failure()).toBeNull();

    const path = await download.path();
    expect(path).not.toBeNull();
    const fs = await import('node:fs/promises');
    const bytes = await fs.readFile(path!);

    expect(bytes.length).toBeGreaterThan(0);
    expect(bytes.subarray(0, 5).toString('latin1')).toBe('%PDF-');
  });
});
