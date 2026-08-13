import { test, expect } from '../fixtures/e2e-test.js';

// docs/testing/strategy.md §7.1 — proves the study queue ranks items correctly: create a
// StudyItem, submit a StudyReview, and see the ranked queue's lead item change accordingly.
// EffectiveScore = (importance/5)*40 + (demand/5)*35 + ((5-mastery)/4)*25 (EffectiveScorePolicy);
// with no EvidenceLinks, demand is 0 for both items. Item A starts at 65 (importance 5, mastery 1)
// and leads over Item B at 45 (importance 4, mastery 3). Reviewing A with confidence 5 raises its
// mastery to 5, dropping its score to 40 — below B's 45 — so B becomes the new lead.
test('creating a StudyItem and submitting a StudyReview changes the study queue ranking', async ({ authenticatedPage: page }) => {
  await test.step('the authenticated queue starts empty', async () => {
    await page.goto('/');
    await expect(page.getByRole('heading', { name: 'No active study items yet' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'New study item' }).first()).toBeVisible();
  });

  await test.step('Item A is created entirely through the UI', async () => {
    await page.getByRole('button', { name: 'New study item' }).first().click();

    await page.getByLabel('Title').fill('Binary search fundamentals');
    await page.getByRole('radiogroup', { name: 'Importance' }).getByRole('radio', { name: '5' }).click();
    await page.getByRole('radiogroup', { name: 'Initial mastery' }).getByRole('radio', { name: '1' }).click();
    await page.getByLabel('Summary').fill('Binary search interview fundamentals.');
    await page.getByRole('button', { name: 'Create' }).click();

    await expect(page.getByRole('heading', { name: 'Binary search fundamentals' })).toBeVisible();
    await page.getByRole('button', { name: 'Back to queue' }).click();
  });

  await test.step('Item B is created entirely through the UI', async () => {
    await page.getByRole('button', { name: 'New study item' }).first().click();

    await page.getByLabel('Title').fill('Two-phase commit protocol');
    await page.getByRole('radiogroup', { name: 'Importance' }).getByRole('radio', { name: '4' }).click();
    await page.getByRole('radiogroup', { name: 'Initial mastery' }).getByRole('radio', { name: '3' }).click();
    await page.getByLabel('Summary').fill('Distributed transaction coordination fundamentals.');
    await page.getByRole('button', { name: 'Create' }).click();

    await expect(page.getByRole('heading', { name: 'Two-phase commit protocol' })).toBeVisible();
    await page.getByRole('button', { name: 'Back to queue' }).click();
  });

  await test.step('the queue shows Item A as the lead', async () => {
    const nextUp = page.getByRole('region', { name: 'Next up' });
    await expect(nextUp.getByRole('heading', { name: 'Binary search fundamentals' })).toBeVisible();

    await nextUp.getByRole('button', { name: 'Open' }).click();
  });

  await test.step('a StudyReview with confidence 5 is submitted for Item A through the UI', async () => {
    await page.getByRole('radiogroup', { name: 'Confidence' }).getByRole('radio', { name: '5' }).click();
    await page.getByRole('button', { name: 'Save review' }).click();

    const reviewHistory = page.getByRole('region', { name: 'Review history' });
    await expect(reviewHistory.getByText(/confidence 5/)).toBeVisible();

    await page.getByRole('button', { name: 'Back to queue' }).click();
  });

  await test.step('the queue reflects the new ranking with Item B as the lead', async () => {
    const nextUp = page.getByRole('region', { name: 'Next up' });
    await expect(nextUp.getByRole('heading', { name: 'Two-phase commit protocol' })).toBeVisible();
  });
});
