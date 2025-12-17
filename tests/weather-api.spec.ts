import { test, expect } from '@playwright/test';

/**
 * Weather Service API Tests
 * Tests the weather service endpoints for functionality and performance
 */
test.describe('Weather Service API', () => {
  test('should return service status', async ({ request }) => {
    // Test the root endpoint
    const response = await request.get('/');
    expect(response.status()).toBe(200);

    const text = await response.text();
    expect(text).toContain('API service is running');
  });

  test('should return weather forecast data', async ({ request }) => {
    // Test the weather forecast endpoint
    const response = await request.get('/weatherforecast');
    expect(response.status()).toBe(200);

    const weatherData = await response.json();
    expect(Array.isArray(weatherData)).toBeTruthy();
    expect(weatherData.length).toBeGreaterThan(0);

    // Verify weather forecast structure
    const forecast = weatherData[0];
    expect(forecast).toHaveProperty('date');
    expect(forecast).toHaveProperty('temperatureC');
    expect(forecast).toHaveProperty('temperatureF');
    expect(forecast).toHaveProperty('summary');
    expect(forecast).toHaveProperty('humidity'); // Custom field from architecture
  });

  test('should return version information', async ({ request }) => {
    // Test the version endpoint
    const response = await request.get('/version');
    expect(response.status()).toBe(200);

    const versionInfo = await response.json();
    expect(versionInfo).toHaveProperty('version');
    expect(versionInfo).toHaveProperty('commitSha');
    expect(versionInfo).toHaveProperty('timestamp');
    expect(versionInfo).toHaveProperty('service');
  });

  test('should return health check', async ({ request }) => {
    // Test the basic health endpoint
    const response = await request.get('/health');
    expect(response.status()).toBe(200);

    const text = await response.text();
    expect(text).toBe('Healthy');
  });

  test('should return detailed health check', async ({ request }) => {
    // Test the detailed health endpoint
    const response = await request.get('/health/detailed');
    expect(response.status()).toBe(200);

    const healthData = await response.json();
    expect(healthData).toMatchObject({
      status: 'healthy',
    });
    expect(healthData).toHaveProperty('version');
    expect(healthData).toHaveProperty('timestamp');
    expect(healthData).toHaveProperty('commitSha');
    expect(healthData).toHaveProperty('uptime');
    expect(healthData).toHaveProperty('features');
  });

  test('should respond within acceptable time limits', async ({ request }) => {
    // Performance test for weather API
    const startTime = Date.now();
    const response = await request.get('/weatherforecast');
    const endTime = Date.now();

    expect(response.status()).toBe(200);
    expect(endTime - startTime).toBeLessThan(1000); // Should respond within 1 second
  });

  test('should handle multiple concurrent requests', async ({ request }) => {
    // Load test with concurrent requests
    const requests = Array(5).fill(0).map(() =>
      request.get('/weatherforecast')
    );

    const responses = await Promise.all(requests);

    responses.forEach(response => {
      expect(response.status()).toBe(200);
    });
  });
});