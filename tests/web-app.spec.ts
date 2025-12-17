import { test, expect } from '@playwright/test';

/**
 * Blazor Web Application Tests
 * Tests the main web application user interface and functionality
 *
 * Note: These tests require the AppHost to be running (dotnet run --project aspire1.AppHost)
 * The Web frontend typically runs on a dynamic port assigned by Aspire (e.g., http://localhost:5142)
 * Set PLAYWRIGHT_WEB_URL to override the default port.
 */
test.describe('Blazor Web Application', () => {
  // Use environment variable for web URL or fallback to HTTPS port
  const webUrl = process.env.PLAYWRIGHT_WEB_URL || 'https://localhost:7296';

  test.beforeEach(async ({ page }) => {
    // Navigate to the web application
    await page.goto(`${webUrl}/`);
  });

  test('should display home page with navigation', async ({ page }) => {
    // Test home page loads properly
    await expect(page).toHaveTitle(/Home/);

    // Check navigation menu exists
    await expect(page.locator('nav')).toBeVisible();

    // Check main navigation links (Blazor NavLinks use relative paths)
    await expect(page.locator('a[href=""]').first()).toBeVisible(); // Home
    await expect(page.locator('a[href="counter"]')).toBeVisible(); // Counter
    await expect(page.locator('a[href="weather"]')).toBeVisible(); // Weather
  });

  test('should navigate to counter page and increment counter', async ({ page }) => {
    // Navigate to counter page
    await page.click('a[href="counter"]');
    await expect(page.locator('h1')).toContainText('Counter');

    // Check initial counter value
    const counterDisplay = page.locator('p[role="status"]');
    await expect(counterDisplay).toContainText('Current count: 0');

    // Click the increment button
    const incrementButton = page.locator('button:has-text("Click me")');
    await incrementButton.click();

    // Verify counter incremented
    await expect(counterDisplay).toContainText('Current count: 1');

    // Click multiple times to test counter functionality
    await incrementButton.click();
    await incrementButton.click();
    await expect(counterDisplay).toContainText('Current count: 3');
  });

  test('should navigate to weather page and display forecast', async ({ page }) => {
    // Navigate to weather page
    await page.click('a[href="weather"]');
    await expect(page.locator('h1')).toContainText('Weather');

    // Wait for weather data to load
    await page.waitForSelector('.weather-card', { timeout: 10000 });

    // Verify weather cards are visible
    const cards = page.locator('.weather-card');
    await expect(cards.first()).toBeVisible();

    // Verify card count (should have multiple weather forecasts)
    const cardCount = await cards.count();
    expect(cardCount).toBeGreaterThan(0);

    // Check first card structure
    const firstCard = cards.first();
    await expect(firstCard.locator('.card-header')).toBeVisible(); // Date header
    await expect(firstCard.locator('.weather-temp')).toBeVisible(); // Temperature
    await expect(firstCard.locator('.weather-summary')).toBeVisible(); // Summary
  });

  test('should handle loading states gracefully', async ({ page }) => {
    // Navigate to weather page and check for loading indicator
    await page.click('a[href="weather"]');

    // Look for loading indicator (if present)
    const loadingIndicator = page.locator('text="Loading..."');
    if (await loadingIndicator.isVisible()) {
      // Wait for loading to complete
      await loadingIndicator.waitFor({ state: 'hidden', timeout: 10000 });
    }

    // Ensure content is loaded
    await expect(page.locator('.weather-card').first()).toBeVisible();
  });

  test('should maintain responsive design on mobile viewport', async ({ page }) => {
    // Set mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });

    // On mobile, nav might be collapsed, so check for navbar toggle
    const navToggle = page.locator('.navbar-toggler');
    if (await navToggle.isVisible()) {
      // Mobile menu is collapsed, this is expected
      await expect(navToggle).toBeVisible();
    }

    // Test counter page on mobile - use direct navigation
    await page.goto(`${webUrl}/counter`);
    await expect(page.locator('button:has-text("Click me")')).toBeVisible();

    // Test weather page on mobile
    await page.goto(`${webUrl}/weather`);
    await page.waitForSelector('.weather-card', { timeout: 10000 });
    await expect(page.locator('.weather-card').first()).toBeVisible();
  });

  test('should validate page health checks', async ({ request }) => {
    // Test web application health endpoint
    const response = await request.get(`${webUrl}/health`);
    expect(response.status()).toBe(200);
  });
});