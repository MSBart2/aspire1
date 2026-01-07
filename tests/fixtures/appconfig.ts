import { test as base, Page } from '@playwright/test';

/**
 * Feature flag configuration for Azure App Configuration mocking
 */
export interface FeatureFlag {
  id: string;
  enabled: boolean;
  description?: string;
}

/**
 * Extended test fixtures with Azure App Configuration mocking capabilities
 */
export const test = base.extend<{
  mockFeatureFlags: (flags: FeatureFlag[]) => Promise<void>;
  enableFeature: (featureId: string) => Promise<void>;
  disableFeature: (featureId: string) => Promise<void>;
}>({
  mockFeatureFlags: async ({ page }, use) => {
    const mockFeatureFlagsImpl = async (flags: FeatureFlag[]) => {
      // Intercept Azure App Configuration requests
      await page.route('**/appconfig/**', async (route) => {
        const url = route.request().url();
        
        // Mock feature flag queries
        if (url.includes('/kv/') && url.includes('.appconfig')) {
          const featureId = extractFeatureId(url);
          const flag = flags.find(f => f.id === featureId);
          
          if (flag) {
            await route.fulfill({
              status: 200,
              contentType: 'application/json',
              body: JSON.stringify({
                value: flag.enabled.toString(),
                key: featureId,
                label: null,
                content_type: 'application/vnd.microsoft.appconfig.ff+json;charset=utf-8',
                last_modified: new Date().toISOString(),
                locked: false,
                tags: {}
              })
            });
          } else {
            await route.fulfill({
              status: 404,
              contentType: 'application/json',
              body: JSON.stringify({ error: 'Feature flag not found' })
            });
          }
        } else {
          await route.continue();
        }
      });
    };
    
    await use(mockFeatureFlagsImpl);
  },

  enableFeature: async ({ page }, use) => {
    const enableFeatureImpl = async (featureId: string) => {
      await page.route(`**/appconfig/**/${featureId}**`, async (route) => {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            value: 'true',
            key: featureId,
            label: null,
            content_type: 'application/vnd.microsoft.appconfig.ff+json;charset=utf-8',
            last_modified: new Date().toISOString(),
            locked: false,
            tags: {}
          })
        });
      });
    };
    
    await use(enableFeatureImpl);
  },

  disableFeature: async ({ page }, use) => {
    const disableFeatureImpl = async (featureId: string) => {
      await page.route(`**/appconfig/**/${featureId}**`, async (route) => {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            value: 'false',
            key: featureId,
            label: null,
            content_type: 'application/vnd.microsoft.appconfig.ff+json;charset=utf-8',
            last_modified: new Date().toISOString(),
            locked: false,
            tags: {}
          })
        });
      });
    };
    
    await use(disableFeatureImpl);
  }
});

export { expect } from '@playwright/test';

/**
 * Helper function to extract feature flag ID from Azure App Config URL
 */
function extractFeatureId(url: string): string {
  const match = url.match(/\/kv\/([^?]+)/);
  return match ? decodeURIComponent(match[1]) : '';
}

/**
 * Preset feature flag configurations for common test scenarios
 */
export const FeatureFlagPresets = {
  /**
   * All features enabled (production-like)
   */
  allEnabled: (): FeatureFlag[] => [
    { id: 'WeatherForecast', enabled: true, description: 'Weather forecast page' },
    { id: 'WeatherHumidity', enabled: true, description: 'Humidity display in weather cards' }
  ],
  
  /**
   * All features disabled
   */
  allDisabled: (): FeatureFlag[] => [
    { id: 'WeatherForecast', enabled: false, description: 'Weather forecast page' },
    { id: 'WeatherHumidity', enabled: false, description: 'Humidity display in weather cards' }
  ],
  
  /**
   * Weather enabled but humidity disabled (partial feature rollout)
   */
  weatherOnlyHumidityOff: (): FeatureFlag[] => [
    { id: 'WeatherForecast', enabled: true, description: 'Weather forecast page' },
    { id: 'WeatherHumidity', enabled: false, description: 'Humidity display in weather cards' }
  ]
};
