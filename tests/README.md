# Playwright Tests for aspire1 .NET Aspire Application

This directory contains comprehensive end-to-end tests for the aspire1 application using Playwright.

## Quick Start

```bash
# 1. Start the aspire1 application
dotnet run --project aspire1.AppHost

# 2. Run all tests (service auto-starts if needed)
npm test

# 3. Run specific test suites
npm run test:api          # Weather API only
npm run test:web          # Web UI only
npm run test:integration  # Service communication
npm run test:performance  # Load and performance

# 4. View HTML report
npm run test:report
```

## Test Suite Overview

### 1. Weather API Tests (`weather-api.spec.ts`)

- **Purpose**: Tests the weather service REST API endpoints
- **Coverage**:
  - Service status endpoint (`/`)
  - Weather forecast data (`/weatherforecast`)
  - Version information (`/version`)
  - Health checks (`/health`, `/health/detailed`)
  - Performance and concurrent request handling
- **Key Validations**:
  - Response structure and data types
  - Custom humidity field from architecture
  - Response time < 1 second
  - Concurrent request handling

### 2. Web Application Tests (`web-app.spec.ts`)

- **Purpose**: Tests the Blazor Server web frontend
- **Coverage**:
  - Home page navigation and layout
  - Counter page functionality with SignalR state
  - Weather page data display and table structure
  - Responsive design on mobile viewports
  - Loading states and error handling
- **Key Validations**:
  - Navigation menu functionality
  - Counter increment behavior
  - Weather data table structure (5 columns including humidity)
  - Mobile responsiveness

### 3. Integration Tests (`integration.spec.ts`)

- **Purpose**: Tests service-to-service communication and end-to-end workflows
- **Coverage**:
  - End-to-end weather data flow (API → UI)
  - Service discovery between services
  - Redis caching performance
  - OpenTelemetry metrics generation
  - SignalR session state persistence
- **Key Validations**:
  - Temperature conversion accuracy (°C ↔ °F)
  - Cache hit performance improvements
  - Session state maintained across navigation
  - Service discovery functionality

### 4. Performance Tests (`performance.spec.ts`)

- **Purpose**: Tests application performance and scalability
- **Coverage**:
  - Page load times
  - Weather data loading performance
  - Rapid navigation stress testing
  - Cache efficiency validation
  - Concurrent user simulation
  - JavaScript bundle size validation
- **Key Validations**:
  - Home page loads < 5 seconds
  - Weather data loads < 3 seconds
  - Bundle size < 5MB (Blazor Server efficiency)
  - Concurrent load handling

## Running Tests

### Prerequisites

1. **Application Running**: The aspire1 AppHost must be running:

   ```bash
   dotnet run --project aspire1.AppHost
   ```

2. **Authentication**: Tests require access to the GitHub Codespaces forwarded ports
   - Use the authenticated URLs provided by the Aspire dashboard
   - Update `playwright.config.ts` baseURL with your codespace URL

### Test Commands

```bash
# Run all tests
npm test

# Run specific test suites
npm run test:api          # Weather API tests only
npm run test:web          # Web application tests only
npm run test:integration  # Integration tests only
npm run test:performance  # Performance tests only

# Run tests with browser UI (debugging)
npm run test:headed

# Debug tests step-by-step
npm run test:debug

# View test results report
npm run test:report
```

### Browser Support

Tests run on:

- Chromium (Chrome/Edge)
- Firefox
- WebKit (Safari)

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
