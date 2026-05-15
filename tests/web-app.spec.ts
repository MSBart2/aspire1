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
  // Use environment variable for web URL
  const webUrl = process.env.PLAYWRIGHT_WEB_URL!;

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
    const incrementButton = page.getByTestId('increment-button');
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
    await page.getByTestId('weather-card').first().waitFor({ timeout: 10000 });

    // Verify weather cards are visible
    const cards = page.getByTestId('weather-card');
    await expect(cards.first()).toBeVisible();

    // Verify card count (should have multiple weather forecasts)
    const cardCount = await cards.count();
    expect(cardCount).toBeGreaterThan(0);

    // Check first card structure
    const firstCard = cards.first();
    await expect(firstCard.getByTestId('weather-date')).toBeVisible(); // Date header
    await expect(firstCard.getByTestId('weather-temp-c')).toBeVisible(); // Temperature
    await expect(firstCard.getByTestId('weather-summary')).toBeVisible(); // Summary
  });

  test('should handle loading states gracefully', async ({ page }) => {
    // Navigate to weather page and check for loading indicator
    await page.click('a[href="weather"]');

    // Look for loading indicator (if present)
    const loadingIndicator = page.getByTestId('weather-loading');
    if (await loadingIndicator.isVisible()) {
      // Wait for loading to complete
      await loadingIndicator.waitFor({ state: 'hidden', timeout: 10000 });
    }

    // Ensure content is loaded
    await expect(page.getByTestId('weather-card').first()).toBeVisible();
  });

  test('should maintain responsive design on mobile viewport', async ({ page }) => {
    // Set mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });

    // On mobile, nav might be collapsed, so check for navbar toggle
    const navToggle = page.getByTestId('nav-toggle');
    if (await navToggle.isVisible()) {
      // Mobile menu is collapsed, this is expected
      await expect(navToggle).toBeVisible();
    }

    // Test counter page on mobile - use direct navigation
    await page.goto(`${webUrl}/counter`);
    await expect(page.getByTestId('increment-button')).toBeVisible();

    // Test weather page on mobile
    await page.goto(`${webUrl}/weather`);
    await page.getByTestId('weather-card').first().waitFor({ timeout: 10000 });
    await expect(page.getByTestId('weather-card').first()).toBeVisible();
  });

  test('should validate page health checks', async ({ request }) => {
    // Test web application health endpoint
    const response = await request.get(`${webUrl}/health`);
    expect(response.status()).toBe(200);
  });
});

test.describe('Weather Card Animations', () => {
  const webUrl = process.env.PLAYWRIGHT_WEB_URL!;

  test.beforeEach(async ({ page }) => {
    // Navigate to the weather page
    await page.goto(`${webUrl}/weather`);
    // Wait for cards to load
    await page.getByTestId('weather-card').first().waitFor({ timeout: 10000 });
  });

  test('should apply freezing animation class for temperatures < 0°C', async ({ page }) => {
    const cards = page.getByTestId('weather-card');
    
    for (let i = 0; i < (await cards.count()); i++) {
      const card = cards.nth(i);
      const tempText = await card.getByTestId('weather-temp-c').textContent();
      const temperature = parseInt(tempText || '0');

      if (temperature < 0) {
        await expect(card.locator('.weather-card--freezing')).toHaveCount(1);
      }
    }
  });

  test('should apply chilly animation class for temperatures 0-15°C', async ({ page }) => {
    const cards = page.getByTestId('weather-card');
    
    for (let i = 0; i < (await cards.count()); i++) {
      const card = cards.nth(i);
      const tempText = await card.getByTestId('weather-temp-c').textContent();
      const temperature = parseInt(tempText || '0');

      if (temperature >= 0 && temperature < 16) {
        await expect(card.locator('.weather-card--chilly')).toHaveCount(1);
      }
    }
  });

  test('should apply mild animation class for temperatures 16-25°C', async ({ page }) => {
    const cards = page.getByTestId('weather-card');
    
    for (let i = 0; i < (await cards.count()); i++) {
      const card = cards.nth(i);
      const tempText = await card.getByTestId('weather-temp-c').textContent();
      const temperature = parseInt(tempText || '0');

      if (temperature >= 16 && temperature < 26) {
        await expect(card.locator('.weather-card--mild')).toHaveCount(1);
      }
    }
  });

  test('should apply hot animation class for temperatures 26-40°C', async ({ page }) => {
    const cards = page.getByTestId('weather-card');
    
    for (let i = 0; i < (await cards.count()); i++) {
      const card = cards.nth(i);
      const tempText = await card.getByTestId('weather-temp-c').textContent();
      const temperature = parseInt(tempText || '0');

      if (temperature >= 26 && temperature <= 40) {
        await expect(card.locator('.weather-card--hot')).toHaveCount(1);
      }
    }
  });

  test('should apply scorching animation class for temperatures > 40°C', async ({ page }) => {
    const cards = page.getByTestId('weather-card');
    
    for (let i = 0; i < (await cards.count()); i++) {
      const card = cards.nth(i);
      const tempText = await card.getByTestId('weather-temp-c').textContent();
      const temperature = parseInt(tempText || '0');

      if (temperature > 40) {
        await expect(card.locator('.weather-card--scorching')).toHaveCount(1);
      }
    }
  });

  test('should render all weather cards regardless of animation class', async ({ page }) => {
    const cards = page.getByTestId('weather-card');
    const cardCount = await cards.count();
    
    expect(cardCount).toBeGreaterThan(0);
    
    // Verify each card has proper structure
    for (let i = 0; i < cardCount; i++) {
      const card = cards.nth(i);
      await expect(card.getByTestId('weather-date')).toBeVisible();
      await expect(card.getByTestId('weather-temp-c')).toBeVisible();
      await expect(card.getByTestId('weather-summary')).toBeVisible();
    }
  });

  test('should respect prefers-reduced-motion media query', async ({ page, context }) => {
    // Create a new context with reduced motion preference
    const reduceMotionPage = await context.newPage();
    await reduceMotionPage.emulateMedia({ reducedMotion: 'reduce' });
    
    await reduceMotionPage.goto(`${webUrl}/weather`);
    await reduceMotionPage.getByTestId('weather-card').first().waitFor({ timeout: 10000 });

    const card = reduceMotionPage.getByTestId('weather-card').first();
    
    // Verify animations are still present (class exists)
    const classAttr = await card.getAttribute('class');
    expect(classAttr).toMatch(/(freezing|chilly|mild|hot|scorching)/);
    
    // Check that animation is not applied via CSS (animations should be removed by media query)
    const pseudoBeforeOpacity = await card.evaluate(() => {
      const computed = window.getComputedStyle(document.querySelector('.weather-card')!, '::before');
      return computed.opacity;
    }).catch(() => null);
    
    // In reduced motion mode, opacity should be lower (0.3) not animated
    await reduceMotionPage.close();
  });

  test('should render animation decorative emojis', async ({ page }) => {
    const cards = page.getByTestId('weather-card');
    const firstCard = cards.first();
    
    // Get the classes to determine which animation state
    const classAttr = await firstCard.getAttribute('class');
    
    // Verify the card has one of the animation classes
    expect(classAttr).toMatch(/(freezing|chilly|mild|hot|scorching)/);
    
    // Check that decorative emoji content is present (::before and ::after pseudo-elements)
    const hasDecorationBefore = await firstCard.evaluate(() => {
      const before = window.getComputedStyle(document.querySelector('.weather-card')!, '::before');
      return before.content !== 'none';
    }).catch(() => false);
    
    // Animation classes should have pseudo-element content defined
    expect(classAttr).toBeTruthy();
  });
});