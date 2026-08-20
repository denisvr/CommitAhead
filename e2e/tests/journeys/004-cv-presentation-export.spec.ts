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
