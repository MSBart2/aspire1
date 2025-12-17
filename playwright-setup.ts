import { spawn, exec } from 'child_process';
import { promisify } from 'util';

const execAsync = promisify(exec);

// Service configuration
const SERVICE_HOST = process.env.PLAYWRIGHT_SERVICE_HOST || '127.0.0.1';
const SERVICE_PORT = process.env.PLAYWRIGHT_SERVICE_PORT || '43141';
const SERVICE_URL = `http://${SERVICE_HOST}:${SERVICE_PORT}`;
const SERVICE_PROJECT = 'aspire1.WeatherService/aspire1.WeatherService.csproj';
const STARTUP_TIMEOUT = 30000; // 30 seconds
const HEALTH_CHECK_INTERVAL = 500; // 500ms

let serviceProcess: any = null;

/**
 * Check if the service is healthy by making a request to its health endpoint
 */
async function isServiceHealthy(): Promise<boolean> {
  try {
    const response = await fetch(`${SERVICE_URL}/health`);
    return response.ok;
  } catch (error) {
    return false;
  }
}

/**
 * Wait for the service to become healthy
 */
async function waitForServiceHealth(): Promise<void> {
  const startTime = Date.now();

  while (Date.now() - startTime < STARTUP_TIMEOUT) {
    if (await isServiceHealthy()) {
      console.log(`✓ WeatherService is healthy at ${SERVICE_URL}`);
      return;
    }
    await new Promise((resolve) => setTimeout(resolve, HEALTH_CHECK_INTERVAL));
  }

  throw new Error(
    `WeatherService did not become healthy within ${STARTUP_TIMEOUT}ms. ` +
    `Check if the service is running or if the port is correct.`
  );
}

/**
 * Start the WeatherService
 */
async function startService(): Promise<void> {
  console.log(`Starting WeatherService on ${SERVICE_URL}...`);

  return new Promise((resolve, reject) => {
    const env = {
      ...process.env,
      ASPNETCORE_URLS: `http://${SERVICE_HOST}:${SERVICE_PORT}`,
    };

    serviceProcess = spawn('dotnet', ['run', '--project', SERVICE_PROJECT, '--no-launch-profile', '--no-build'], {
      cwd: process.cwd(),
      env,
      stdio: ['pipe', 'pipe', 'pipe'], // Capture stdout and stderr
    });

    let startupErrorOutput = '';

    serviceProcess.stderr?.on('data', (data: Buffer) => {
      const output = data.toString();
      startupErrorOutput += output;
      // Log important startup messages
      if (output.includes('Now listening on:') || output.includes('Application started')) {
        console.log(`[WeatherService] ${output.trim()}`);
      }
    });

    serviceProcess.stdout?.on('data', (data: Buffer) => {
      const output = data.toString();
      if (output.includes('Now listening on:') || output.includes('Application started')) {
        console.log(`[WeatherService] ${output.trim()}`);
      }
    });

    serviceProcess.on('error', (error: Error) => {
      reject(new Error(`Failed to start WeatherService: ${error.message}`));
    });

    serviceProcess.on('exit', (code: number) => {
      if (code !== 0 && code !== null) {
        reject(
          new Error(
            `WeatherService exited with code ${code}. ` +
            `Error output:\n${startupErrorOutput}`
          )
        );
      }
    });

    // Give the process a moment to start, then check health
    setTimeout(() => {
      waitForServiceHealth()
        .then(resolve)
        .catch(reject);
    }, 1000);
  });
}

/**
 * Global setup: ensure service is running before tests start
 */
async function globalSetup(): Promise<void> {
  console.log('\n🚀 Playwright Global Setup Started\n');

  try {
    // Check if service is already running
    const isHealthy = await isServiceHealthy();

    if (isHealthy) {
      console.log(`✓ WeatherService is already running at ${SERVICE_URL}`);
      return;
    }

    console.log(`✗ WeatherService is not responding at ${SERVICE_URL}`);

    // Try to start the service
    await startService();
  } catch (error) {
    console.error('\n❌ Global Setup Failed:', error);
    throw error;
  }
}

export default globalSetup;
