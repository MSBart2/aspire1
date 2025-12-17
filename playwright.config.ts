import { defineConfig, devices } from '@playwright/test';

/**
 * @see https://playwright.dev/docs/test-configuration
 */
export default defineConfig({
  testDir: './tests',
  /* Run tests in files in parallel */
  fullyParallel: true,
  /* Fail the build on CI if you accidentally left test.only in the source code. */
  forbidOnly: !!process.env.CI,
  /* Retry on CI only */
  retries: process.env.CI ? 2 : 0,
  /* Opt out of parallel tests on CI. */
  workers: process.env.CI ? 1 : undefined,
  /* Reporter to use. See https://playwright.dev/docs/test-reporters */
  reporter: 'html',
  /* Global setup and teardown */
  globalSetup: require.resolve('./playwright-setup.ts'),
  /* Shared settings for all the projects below. See https://playwright.dev/docs/api/class-testoptions. */
  use: {
    /* Base URL to use in actions like `await page.goto('/')`.
     * Defaults to http://127.0.0.1:43141 where WeatherService runs.
     * Override with PLAYWRIGHT_BASE_URL environment variable.
     */
    baseURL: process.env.PLAYWRIGHT_BASE_URL || 'http://127.0.0.1:43141',

    /* Collect trace when retrying the failed test. See https://playwright.dev/docs/trace-viewer */
    trace: 'on-first-retry',
    screenshot: 'always', // Takes screenshots for all tests

    /* Increase timeout for potentially slow GitHub Codespaces environment */
    timeout: 30 * 1000, // 30 seconds

    /* Ignore HTTPS certificate errors for self-signed development certificates */
    ignoreHTTPSErrors: true,

    /* Extra HTTP headers for authentication if needed */
    extraHTTPHeaders: {
      'Accept': 'application/json, text/html',
    },
  },

  /* Global timeout for each test */
  timeout: 60 * 1000, // 60 seconds

  /* Configure projects for major browsers */
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    }
  ],

  /* Run your local dev server before starting the tests */
  // webServer: {
  //   command: 'dotnet run --project aspire1.AppHost',
  //   url: 'http://localhost:5000', // Aspire dashboard port
  //   reuseExistingServer: !process.env.CI,
  //   timeout: 120 * 1000, // 2 minutes to start
  // },
});