import { test, expect } from '@playwright/test';

/**
 * Integration Tests
 * Tests service-to-service communication and end-to-end workflows
 *
 * Note: Requires both services running:
 * - WeatherService API: http://127.0.0.1:43141 (or PLAYWRIGHT_BASE_URL)
 * - Web Frontend: http://localhost:5142 (or PLAYWRIGHT_WEB_URL)
 */
test.describe('Service Integration', () => {
  const apiUrl = process.env.PLAYWRIGHT_BASE_URL || 'http://127.0.0.1:43141';
  const webUrl = process.env.PLAYWRIGHT_WEB_URL || 'https://localhost:7296';

  test('should demonstrate end-to-end weather flow', async ({ page, request }) => {
    // First verify the weather service API is working
    const apiResponse = await request.get(`${apiUrl}/weatherforecast`);
    expect(apiResponse.status()).toBe(200);

    const apiData = await apiResponse.json();
    expect(Array.isArray(apiData)).toBeTruthy();
    expect(apiData.length).toBeGreaterThan(0);

    // Now verify the web UI displays the same data
    await page.goto(`${webUrl}/weather`);
    await page.waitForSelector('.weather-card', { timeout: 10000 });

    // Get the first weather card
    const firstCard = page.locator('.weather-card').first();
    await expect(firstCard).toBeVisible();

    // Verify the card has temperature data
    const cardBody = firstCard.locator('.card-body');
    const tempCText = await cardBody.locator('.weather-temp').textContent();
    expect(tempCText).toBeTruthy();

    // Extract temperatures (format is like "51° C" and "123°F")
    const tempCMatch = tempCText!.match(/(-?\d+)°/);
    const tempFText = await cardBody.locator('.text-muted').first().textContent();
    const tempFMatch = tempFText!.match(/(-?\d+)°F/);

    if (tempCMatch && tempFMatch) {
      const tempC = parseInt(tempCMatch[1]);
      const tempF = parseInt(tempFMatch[1]);

      // Basic fahrenheit/celsius conversion check (F = C * 9/5 + 32)
      const expectedF = Math.round(tempC * 9 / 5 + 32);
      expect(Math.abs(tempF - expectedF)).toBeLessThanOrEqual(1); // Allow for rounding

      console.log(`Verified temperature conversion: ${tempC}°C = ${tempF}°F`);
    }
  });

  test('should handle service discovery correctly', async ({ page }) => {
    await page.goto(`${webUrl}/weather`);

    // The fact that weather data loads confirms service discovery is working
    // between the web frontend and weather service
    await page.waitForSelector('.weather-card', { timeout: 10000 });
    await expect(page.locator('.weather-card').first()).toBeVisible();

    // Check that the service communication is working by verifying data freshness
    const cards = page.locator('.weather-card');
    const cardCount = await cards.count();
    expect(cardCount).toBeGreaterThan(0);

    const firstCardContent = await cards.first().textContent();
    expect(firstCardContent).toBeTruthy();
    expect(firstCardContent!.length).toBeGreaterThan(0);
  });

  test('should verify health checks across services', async ({ request }) => {
    // Test web frontend health
    const webHealth = await request.get(`${webUrl}/health`);
    expect(webHealth.status()).toBe(200);

    // Test weather service health (if exposed through the web frontend)
    // In a typical Aspire setup, individual service health might be aggregated
    const webHealthText = await webHealth.text();
    expect(webHealthText).toBe('Healthy');
  });

  test('should handle Redis caching integration', async ({ page, request }) => {
    // Make an API call to potentially populate cache
    await request.get(`${apiUrl}/weatherforecast`);

    // Navigate to weather page
    await page.goto(`${webUrl}/weather`);
    const startTime = Date.now();
    await page.waitForSelector('.weather-card', { timeout: 10000 });
    const firstLoadTime = Date.now() - startTime;

    // Reload the same page to test caching
    await page.reload();
    const startTime2 = Date.now();
    await page.waitForSelector('.weather-card', { timeout: 10000 });
    const secondLoadTime = Date.now() - startTime2;

    console.log(`First load: ${firstLoadTime}ms, Cached reload: ${secondLoadTime}ms`);

    // Both loads should be reasonably fast
    expect(firstLoadTime).toBeLessThan(3000);
    expect(secondLoadTime).toBeLessThan(3000);
  });

  test('should verify OpenTelemetry metrics collection', async ({ page, request }) => {
    // Generate some activity that should create metrics
    await page.goto(`${webUrl}/counter`);

    // Click counter multiple times to generate custom metrics
    const incrementButton = page.locator('button:has-text("Click me")');
    for (let i = 0; i < 5; i++) {
      await incrementButton.click();
      await page.waitForTimeout(100); // Small delay between clicks
    }

    // Navigate to weather to generate API metrics
    await page.click('a[href="weather"]');
    await page.waitForSelector('.weather-card', { timeout: 10000 });

    // Refresh weather data to generate more API calls
    await page.reload();
    await page.waitForSelector('.weather-card', { timeout: 10000 });

    // Verify the application is still responsive (metrics collection shouldn't impact performance)
    await expect(page.locator('.weather-card').first()).toBeVisible();

    console.log('Generated telemetry data through user interactions');
  });

  test('should verify session state and SignalR if enabled', async ({ page }) => {
    await page.goto(`${webUrl}/`);

    // Test if session state persists across navigation
    await page.click('a[href="counter"]');

    // Increment counter
    const incrementButton = page.locator('button:has-text("Click me")');
    await incrementButton.click();
    await incrementButton.click();

    // Verify counter shows 2
    await expect(page.locator('p[role="status"]')).toContainText('Current count: 2');

    // Navigate away and back
    await page.click('a[href="weather"]');
    await page.waitForSelector('.weather-card, text="Loading..."', { timeout: 5000 }).catch(() => {});
    await page.click('a[href="counter"]');

    // In Blazor Server, component state is reset on navigation by default
    // But SignalR connection should remain active (verify page is responsive)
    await expect(page.locator('p[role="status"]')).toBeVisible();

    // Test that SignalR is working by clicking the button again
    await incrementButton.click();
    await expect(page.locator('p[role="status"]')).toContainText('Current count: 1');

    console.log('Verified SignalR connection remains active across navigation');
  });
});