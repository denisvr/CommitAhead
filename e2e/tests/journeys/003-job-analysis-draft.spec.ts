import { test, expect } from '../fixtures/e2e-test.js';

// docs/testing/strategy.md §7.1 — proves AI commands produce valid AnalysisDrafts and apply
// accepted proposals: create a pasted-text JobAnalysis, Analyze it (the real AnthropicAIProvider
// against the deterministic external-stub, §7.6), review the draft, accept one
// AddJobRequirement/AddJobGap pair and reject another, Apply, and confirm the accepted pair's
// effects land on the JobAnalysis while the rejected pair's do not. Never a PDF upload — pasted
// text only (§7.1).
test('creating and analyzing a JobAnalysis produces a draft whose accepted proposals apply and rejected proposals do not', async ({ authenticatedPage: page }) => {
  const title = 'Acme — Senior Backend Engineer';

  await test.step('a pasted-text JobAnalysis is created entirely through the UI', async () => {
    await page.goto('/');
    await page.getByRole('button', { name: 'Job analyses' }).click();
    await page.getByRole('button', { name: 'New job analysis' }).first().click();

    await page.getByLabel('Title').fill(title);
    await page
      .getByLabel('Job posting text')
      .fill(
        'Backend Engineer — Distributed Systems\n\nWe need someone experienced with distributed caching, GraphQL APIs, and container orchestration.',
      );
    await page.getByRole('button', { name: 'Create' }).click();

    await expect(page.getByRole('heading', { name: title })).toBeVisible();
  });

  await test.step('Analyze navigates to the review page for the new draft', async () => {
    await page.getByRole('button', { name: 'Analyze' }).click();
    await expect(page.getByRole('heading', { name: 'Review analysis draft' })).toBeVisible();
  });

  await test.step('the accepted AddJobRequirement/AddJobGap pair is decided', async () => {
    const requirementCard = page
      .getByRole('listitem')
      .filter({ hasText: 'Must have hands-on experience designing and implementing cache invalidation strategies at scale.' });
    await requirementCard.getByRole('button', { name: 'Accept' }).click();

    const gapCard = page
      .getByRole('listitem')
      .filter({ hasText: "No cache invalidation work is documented in the candidate's profile or study catalogue." });
    await gapCard.getByRole('button', { name: 'Accept' }).click();
  });

  await test.step('the rejected AddJobRequirement/AddJobGap pair is decided', async () => {
    const requirementCard = page.getByRole('listitem').filter({ hasText: 'Experience with GraphQL is a plus but not required.' });
    await requirementCard.getByRole('button', { name: 'Reject' }).click();

    const gapCard = page.getByRole('listitem').filter({ hasText: 'Some API design experience exists but no direct GraphQL exposure.' });
    await gapCard.getByRole('button', { name: 'Reject' }).click();
  });

  await test.step('Apply commits the accepted effects onto the JobAnalysis and omits the rejected ones', async () => {
    await page.getByRole('button', { name: 'Apply' }).click();
    await expect(page.getByRole('heading', { name: title })).toBeVisible();

    const requirements = page.getByRole('region', { name: 'Requirements' });
    const gaps = page.getByRole('region', { name: 'Gaps' });

    await expect(requirements.getByText('Design and implement cache invalidation strategies for distributed systems')).toBeVisible();
    await expect(gaps.getByText("No cache invalidation work is documented in the candidate's profile or study catalogue.")).toBeVisible();

    await expect(requirements.getByText('Familiarity with GraphQL API design')).toHaveCount(0);
    await expect(gaps.getByText('Some API design experience exists but no direct GraphQL exposure.')).toHaveCount(0);
  });
});
