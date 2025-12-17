# 🎭 Playwright E2E Tests for aspire1

> _Because clicking buttons manually is so 2020_ ✨

This directory contains comprehensive **end-to-end (E2E) tests** for the aspire1 application using [Playwright](https://playwright.dev). We're testing the whole love triangle: Browser → Web Frontend → API, all in **3 browsers simultaneously** (Chromium, Firefox, WebKit).

Think of these as automated QA engineers that never get tired, never forget anything, and—best of all—never complain about finding bugs. 🤖

## Quick Start

The fastest way to run E2E tests (auto-starts services):

```bash
# 1. Start the Aspire AppHost (orchestrates everything)
dotnet run --project aspire1.AppHost

# 2. In another terminal, run all tests
npm test
# (or just: npm ci && npm test)

# 3. View the gorgeous HTML report
npm run test:report
```

**That's it.** 🎉 No manual service startup, no port conflicts, no excuses.

## Test Suite Overview

### 1. 🌤️ Weather API Tests (`weather-api.spec.ts`)

Tests the REST API backend with surgical precision.

**Coverage:**

- `GET /` - Service status endpoint
- `GET /weatherforecast` - Main data endpoint with caching
- `GET /version` - Version metadata
- `GET /health` & `GET /health/detailed` - Health checks with feature flags
- Concurrent request handling (Redis cache distribution)
- Response time validation (< 1 second guaranteed)

**Key Validations:**

- Response structure matches architecture (including custom humidity field 💧)
- HTTP status codes (200 OK, 400 Bad Request, etc.)
- JSON schema validation (no sneaky fields appearing)
- Performance metrics tracking
- Cache behavior (hits vs. misses)

**Why This Matters:**
If the API breaks, nothing else matters. This test suite ensures the heart keeps beating.

### 2. 🎨 Web Application Tests (`web-app.spec.ts`)

Tests the Blazor Server UI with all its interactive glory.

**Coverage:**

- Home page navigation and layout
- Counter page with SignalR state management (click to your heart's content 🖱️)
- Weather page with beautiful card display
- Navigation menu functionality
- Responsive design (desktop → tablet → mobile)
- Loading states and error handling

**Key Validations:**

- Navigation links lead where promised
- Counter increments work (and stay incremented across page navigations)
- Weather table structure: Date, Temp °C, Temp °F, Summary, Humidity (feature-flagged)
- Mobile layout adapts correctly (3 cols → 2 cols → 1 col)
- Error messages appear when they should

**Why This Matters:**
Pretty UI is nice, but usable UI is everything. Users don't care about your architecture—they care that the buttons work.

### 3. 🔗 Integration Tests (`integration.spec.ts`)

Tests the entire **application flow** end-to-end (Web → API → Caching → Metrics).

**Coverage:**

- End-to-end weather data flow: API → Redis Cache → Blazor UI
- Service discovery between Web and WeatherService
- Redis caching performance (first hit vs. subsequent hits)
- OpenTelemetry metrics generation during user workflows
- SignalR session state persistence across navigation
- Feature flag propagation (disable humidity, watch UI update)

**Key Validations:**

- Temperature conversion accuracy (°C ↔ °F match expected values)
- Cache hit performance: Second load is ~70% faster than first load
- Session state maintained when navigating: Counter value persists 💪
- Service discovery: No hard-coded URLs, all dynamic
- Metrics flowing to Application Insights

**Why This Matters:**
Unit tests pass but integration fails? This test suite catches that nightmare scenario before prod sees it.

### 4. ⚡ Performance Tests (`performance.spec.ts`)

Tests speed, scalability, and user experience under realistic conditions.

**Coverage:**

- Page load times (home, weather, counter pages)
- Weather data loading performance (< 3 seconds)
- Rapid navigation stress testing (bounce between pages rapidly)
- Cache efficiency validation (second loads are faster)
- Concurrent user simulation (5 users hitting simultaneously)
- JavaScript bundle size validation (Blazor Server efficiency check 📦)

**Key Validations:**

- Home page loads < 5 seconds (no excuses)
- Weather data loads < 3 seconds (nobody has time for spinning loaders)
- Bundle size < 5MB (Blazor Server slimness verified)
- Concurrent load handling: 5 users, no 500 errors
- Cache prevents thundering herd problem

**Why This Matters:**
Fast beats pretty. A slow app is a dead app. These tests ensure users don't abandon you at the loading screen.

## Running Tests

### Prerequisites

1. **Application Running** (Aspire AppHost orchestrates everything):

   ```bash
   dotnet run --project aspire1.AppHost
   # Dashboard at https://localhost:15888
   ```

2. **Node.js & npm** (for Playwright):

   ```bash
   # Should already be in your dev container, but just in case:
   node --version  # v18 or higher
   npm --version   # v9 or higher
   ```

3. **Dependencies Installed**:

   ```bash
   npm ci  # Install exact versions from package-lock.json
   ```

## Running Tests

### All Tests

```bash
# Run everything across all 3 browsers (takes ~1-2 min)
npm test
```

### Test Suite Commands

```bash
# Run specific test suites
npm run test:api          # 🌤️ API endpoints only (~20 seconds)
npm run test:web          # 🎨 UI interactions only (~30 seconds)
npm run test:integration  # 🔗 Full flows only (~45 seconds)
npm run test:performance  # ⚡ Load & speed only (~60 seconds)

# Run all with browser UI visible (great for debugging)
npm run test:headed

# Step through tests one action at a time (detective mode)
npm run test:debug

# View the gorgeous HTML test report
npm run test:report
```

### Custom Filters

```bash
# Run only one test by name
npx playwright test -g "should load home page"

# Run only one test file
npx playwright test weather-api.spec.ts

# Run tests in one specific browser
npx playwright test --project=chromium
npx playwright test --project=firefox
npx playwright test --project=webkit

# Update snapshots (if visual comparison tests fail)
npx playwright test --update-snapshots
```

### Browser Support

Tests run on **3 browsers automatically**:

- **Chromium** (Chrome/Edge) - Default for CI/CD
- **Firefox** - For cross-browser compatibility
- **WebKit** (Safari) - For the Apple ecosystem

Each test runs against all 3, so you catch browser-specific bugs before users do. 👍

## Configuration

### Automated Service Startup

The `playwright-setup.ts` file implements [global setup](https://playwright.dev/docs/test-global-setup-teardown) which:

1. **Checks if WeatherService is healthy** by making a request to `http://127.0.0.1:43141/health`
2. **Automatically starts the service** if not running (spawns `dotnet run`)
3. **Waits for startup** with health checks until service responds (30s timeout)
4. **Runs all tests** against the live service

### Environment Variables

Customize test behavior with these variables:

| Variable                  | Default                  | Purpose                                          |
| ------------------------- | ------------------------ | ------------------------------------------------ |
| `PLAYWRIGHT_BASE_URL`     | `http://127.0.0.1:43141` | Base URL for test requests                       |
| `PLAYWRIGHT_WEB_URL`      | `https://localhost:7296` | Web frontend URL                                 |
| `PLAYWRIGHT_SERVICE_HOST` | `127.0.0.1`              | Host where WeatherService runs                   |
| `PLAYWRIGHT_SERVICE_PORT` | `43141`                  | Port where WeatherService runs                   |
| `PLAYWRIGHT_KILL_SERVICE` | `false`                  | Kill service after tests (default: keep running) |

#### Examples

```bash
# Run tests against custom service location
PLAYWRIGHT_SERVICE_HOST=192.168.1.100 PLAYWRIGHT_SERVICE_PORT=5000 npm run test:api

# Stop service automatically after tests
PLAYWRIGHT_KILL_SERVICE=true npm run test:api

# Override base URL for remote deployment testing
PLAYWRIGHT_BASE_URL=https://myservice.azurewebsites.net npm run test:api
```

### Base URL Configuration

Update the `baseURL` in `playwright.config.ts` for your environment:

```typescript
use: {
  baseURL: 'https://your-codespace-url.app.github.dev',
  ignoreHTTPSErrors: true,  // For self-signed dev certificates
}
```

## System Requirements

### Browser Dependencies

Install required system packages for Chromium:

```bash
# Install Playwright browser dependencies
npx playwright install-deps chromium

# Or install all browser dependencies
npx playwright install --with-deps

# Minimal dependency install (if needed)
sudo apt-get install -y libnspr4-dev
```

### Dev Container Setup

Add to `.devcontainer/devcontainer.json` for automatic setup:

```json
{
  "postCreateCommand": "npm install && npx playwright install --with-deps chromium"
}
```

## Test Architecture Alignment

These tests are designed specifically for the aspire1 architecture:

### Service Discovery Testing

- Tests validate that the web frontend can communicate with the weather service
- Uses the service names defined in `AppHost.cs` (`weatherservice`, `webfrontend`)

### Custom Telemetry Validation

- Tests generate activity that triggers the 6 custom metrics:
  - `counter.clicks` (Counter page interactions)
  - `weather.api.calls` (Weather API usage)
  - `weather.sunny.count` (Weather data analysis)
  - `cache.hits/misses` (Redis cache validation)
  - `api.call.duration` (Performance metrics)

### Feature Flags Integration

- Tests check for feature flag configuration in health endpoints
- Validates Azure App Configuration integration

### Redis Caching Verification

- Tests cache performance by measuring load times
- Validates cache hit vs. miss scenarios
- Tests session state persistence

## CI/CD Integration

### GitHub Actions Integration

Tests can be integrated into the existing CI/CD pipeline:

```yaml
- name: Run Playwright Tests
  run: |
    npm ci
    npx playwright install --with-deps
    npm test
  env:
    CI: true
```

### Azure DevOps Integration

```yaml
- task: Npm@1
  inputs:
    command: "ci"
- task: Npm@1
  inputs:
    command: "custom"
    customCommand: "run test"
```

## Troubleshooting

### Service Startup Issues

**Tests fail with "ECONNREFUSED"**

```bash
# Check if port is in use
lsof -i :43141

# Kill existing process if needed
kill -9 <PID>

# Try different port
PLAYWRIGHT_SERVICE_PORT=5337 npm run test:api
```

**Service fails to start**

```bash
# Verify project builds
dotnet build aspire1.WeatherService

# Check for build errors
dotnet build aspire1.sln

# Increase timeout in playwright-setup.ts if needed
```

### Browser Launch Failures

**Missing Chromium dependencies**

```bash
# Error: libnss3.so: cannot open shared object file
npx playwright install-deps chromium
```

**HTTPS certificate errors**

- Add `ignoreHTTPSErrors: true` to `playwright.config.ts`
- Or install dev certificate: `dotnet dev-certs https --trust`

### Common Issues

1. **Authentication Errors (401)**

   - Ensure you're using the correct authenticated codespace URL
   - Check that the application is running and accessible

2. **Timeout Errors**

   - Increase timeout values in test configuration
   - Check network connectivity to the application

3. **Service Discovery Failures**

   - Verify AppHost is running and all services are healthy
   - Check Aspire dashboard for service status

4. **Data Validation Failures**
   - Verify weather API is returning expected data structure
   - Check that custom humidity field is included in responses

### Debug Mode

Run tests with detailed output:

```bash
npm run test:debug        # Step-by-step debugging
npm run test:headed       # Show browser UI
npm run test:report       # View detailed HTML report
```

### Performance Expectations

- **API Response Times**: < 1 second for weather endpoints
- **Page Load Times**: < 5 seconds for initial page load
- **Weather Data Load**: < 3 seconds for weather table display
- **Cache Performance**: Second loads should be 50% faster than first loads

## Extending Tests

### Adding New Test Scenarios

1. Create new `.spec.ts` file in the tests directory
2. Follow the existing test structure and naming conventions
3. Update `package.json` scripts if needed

### Test Data Management

- Tests use live application data
- For deterministic testing, consider adding test data endpoints
- Mock external dependencies if needed for isolation

### Monitoring Integration

- Tests generate telemetry data that appears in Application Insights
- Monitor test runs for performance trends
- Use custom metrics to track test execution patterns

## Architecture Documentation References

- **Overall Architecture**: `/ARCHITECTURE.md`
- **AppHost Configuration**: `/aspire1.AppHost/ARCHITECTURE.md`
- **Weather Service**: `/aspire1.WeatherService/ARCHITECTURE.md`
- **Web Frontend**: `/aspire1.Web/ARCHITECTURE.md`
- **Service Defaults**: `/aspire1.ServiceDefaults/ARCHITECTURE.md`
