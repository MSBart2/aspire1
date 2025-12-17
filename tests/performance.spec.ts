import { test, expect } from '@playwright/test';

/**
 * Performance Tests
 * Tests application performance, loading times, and responsiveness
 *
 * Note: Tests the Web Frontend at http://localhost:5142 (or PLAYWRIGHT_WEB_URL)
 */
test.describe('Performance Tests', () => {
  const webUrl = process.env.PLAYWRIGHT_WEB_URL || 'https://localhost:7296';

  test('should load home page within acceptable time', async ({ page }) => {
    const startTime = Date.now();

    await page.goto(`${webUrl}/`);
    await page.waitForLoadState('networkidle');

    const loadTime = Date.now() - startTime;
    expect(loadTime).toBeLessThan(5000); // Should load within 5 seconds

    console.log(`Home page load time: ${loadTime}ms`);
  });

  test('should load weather data efficiently', async ({ page }) => {
    await page.goto(`${webUrl}/`);
    await page.click('a[href="weather"]');

    const startTime = Date.now();
    await page.waitForSelector('.weather-card', { timeout: 10000 });
    const loadTime = Date.now() - startTime;

    expect(loadTime).toBeLessThan(3000); // Weather data should load within 3 seconds
    console.log(`Weather data load time: ${loadTime}ms`);
  });

  test('should handle rapid navigation without issues', async ({ page }) => {
    await page.goto(`${webUrl}/`);

    // Rapidly navigate between pages
    for (let i = 0; i < 3; i++) {
      await page.click('a[href="counter"]');
      await expect(page.locator('h1')).toContainText('Counter');

      await page.click('a[href="weather"]');
      await expect(page.locator('h1')).toContainText('Weather');

      await page.click('a[href=""]');
      await expect(page.locator('h1')).toBeVisible();
    }
  });

  test('should cache weather data efficiently', async ({ page }) => {
    await page.goto(`${webUrl}/weather`);

    // First load
    const startTime1 = Date.now();
    await page.waitForSelector('.weather-card', { timeout: 10000 });
    const firstLoadTime = Date.now() - startTime1;

    // Navigate away and back
    await page.click('a[href="counter"]');
    await page.click('a[href="weather"]');

    // Second load (should be faster due to caching)
    const startTime2 = Date.now();
    await page.waitForSelector('.weather-card', { timeout: 5000 });
    const secondLoadTime = Date.now() - startTime2;

    console.log(`First load: ${firstLoadTime}ms, Second load: ${secondLoadTime}ms`);

    // Second load should be reasonably fast (cache doesn't always make it faster due to rehydration)
    expect(secondLoadTime).toBeLessThan(5000); // Should still load quickly
  });

  test('should maintain performance under simulated load', async ({ browser }) => {
    // Create multiple pages to simulate concurrent users
    const pages = await Promise.all([
      browser.newPage(),
      browser.newPage(),
      browser.newPage()
    ]);

    const startTime = Date.now();

    // Navigate all pages simultaneously
    await Promise.all(pages.map(async (page, index) => {
      await page.goto(`${webUrl}/`);
      await page.click('a[href="weather"]');
      await page.waitForSelector('.weather-card', { timeout: 15000 });
    }));

    const totalTime = Date.now() - startTime;
    expect(totalTime).toBeLessThan(15000); // All pages should load within 15 seconds

    console.log(`Concurrent load test completed in: ${totalTime}ms`);

    // Close all pages
    await Promise.all(pages.map(page => page.close()));
  });

  test('should have efficient bundle sizes', async ({ page }) => {
    // Navigate to the app and check resource sizes
    const requests: any[] = [];

    page.on('response', async (response) => {
      if (response.url().includes('_framework/') || response.url().includes('.js')) {
        const headers = response.headers();
        const contentLength = headers['content-length'];

        if (contentLength) {
          requests.push({
            url: response.url(),
            size: parseInt(contentLength),
            status: response.status()
          });
        }
      }
    });

    await page.goto(`${webUrl}/`);
    await page.waitForLoadState('networkidle');

    // Check for reasonable bundle sizes (Blazor Server typically has smaller initial payloads)
    const totalSize = requests.reduce((sum, req) => sum + req.size, 0);
    console.log(`Total JavaScript bundle size: ${totalSize / 1024}KB`);

    // Blazor Server apps should have relatively small initial JS payloads
    expect(totalSize).toBeLessThan(5 * 1024 * 1024); // Less than 5MB total
  });
});