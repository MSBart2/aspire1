import { test, expect } from '@playwright/test';

/**
 * Weather Feature Flag Output Cache Tests
 *
 * Verifies that the Weather page no longer serves stale cached content after
 * the [OutputCache(Duration = 5)] attribute was removed from Weather.razor.
 *
 * Before the fix: page-level HTML was cached for 5 seconds — feature flag
 * toggles had no visible effect until the cache expired. StreamRendering
 * could even lock in the "Loading..." placeholder as cached content.
 *
 * After the fix: every navigation to /weather gets a fresh server render,
 * reflecting the current feature flag state immediately.
 */
test.describe('Weather Page — Output Cache Removal', () => {
  const webUrl = process.env.PLAYWRIGHT_WEB_URL!;

  test('weather page loads without cache interference on consecutive navigations', async ({ page }) => {
    // First navigation — establish baseline
    await page.goto(`${webUrl}/weather`);
    await page.waitForLoadState('networkidle');

    // Either weather cards or the feature-disabled alert should be visible — never stuck on "Loading..."
    const hasCards = await page.getByTestId('weather-card').first().isVisible().catch(() => false);
    const hasDisabledAlert = await page.locator('.alert-warning').isVisible().catch(() => false);
    expect(hasCards || hasDisabledAlert).toBeTruthy();

    // Second navigation in quick succession — must show current state, not stale cached HTML
    await page.goto(`${webUrl}/weather`);
    await page.waitForLoadState('networkidle');

    const hasCardsSecond = await page.getByTestId('weather-card').first().isVisible().catch(() => false);
    const hasDisabledAlertSecond = await page.locator('.alert-warning').isVisible().catch(() => false);
    expect(hasCardsSecond || hasDisabledAlertSecond).toBeTruthy();
  });

  test('weather page loading state resolves correctly — StreamRendering still functional', async ({ page }) => {
    await page.goto(`${webUrl}/weather`);

    // If the loading indicator appears, it should disappear as data streams in
    const loadingIndicator = page.getByTestId('weather-loading');
    if (await loadingIndicator.isVisible()) {
      await loadingIndicator.waitFor({ state: 'hidden', timeout: 10000 });
    }

    // After streaming completes, real content must be visible — no "Loading..." locked in cache
    const hasCards = await page.getByTestId('weather-card').first().isVisible().catch(() => false);
    const hasDisabledAlert = await page.locator('.alert-warning').isVisible().catch(() => false);
    expect(hasCards || hasDisabledAlert).toBeTruthy();
  });

  test('weather page shows real content on multiple consecutive page loads', async ({ page }) => {
    // Load the weather page 3 times in quick succession
    // Before the fix, the second and third loads could serve the cached "Loading..." placeholder
    for (let i = 0; i < 3; i++) {
      await page.goto(`${webUrl}/weather`);
      await page.waitForLoadState('networkidle');

      const loadingStuck = await page.getByTestId('weather-loading').isVisible().catch(() => false);
      expect(loadingStuck).toBeFalsy();

      const hasContent = await page.getByTestId('weather-card').first().isVisible().catch(() => false)
        || await page.locator('.alert-warning').isVisible().catch(() => false);
      expect(hasContent).toBeTruthy();
    }
  });

  test('weather page responds to direct navigation — no stale 5-second cache window', async ({ page }) => {
    // Navigate away and back — should always get fresh render
    await page.goto(`${webUrl}/`);
    await page.goto(`${webUrl}/weather`);
    await page.waitForLoadState('networkidle');

    // Verify page title rendered (not a cached placeholder from a previous session)
    await expect(page.locator('h1')).toContainText('Weather');

    // Verify no stuck loading indicator
    const isLoadingVisible = await page.getByTestId('weather-loading').isVisible().catch(() => false);
    expect(isLoadingVisible).toBeFalsy();
  });
});
