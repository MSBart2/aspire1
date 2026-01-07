import { spawn, exec } from 'child_process';
import { promisify } from 'util';
import * as fs from 'fs';
import * as path from 'path';
import * as os from 'os';

const execAsync = promisify(exec);

// PID file to persist process IDs across setup/teardown
const PID_FILE = path.join(os.tmpdir(), 'playwright-aspire-pids.json');

// WeatherService API configuration
const SERVICE_HOST = process.env.PLAYWRIGHT_SERVICE_HOST || '127.0.0.1';
const SERVICE_PORT = process.env.PLAYWRIGHT_SERVICE_PORT || '43141';
const SERVICE_URL = `http://${SERVICE_HOST}:${SERVICE_PORT}`;
const SERVICE_PROJECT = 'aspire1.WeatherService/aspire1.WeatherService.csproj';

// Web Frontend configuration
const WEB_HOST = process.env.PLAYWRIGHT_WEB_HOST || 'localhost';
const WEB_PORT = process.env.PLAYWRIGHT_WEB_PORT || '5142';
const WEB_URL = process.env.PLAYWRIGHT_WEB_URL || `http://${WEB_HOST}:${WEB_PORT}`;
const WEB_PROJECT = 'aspire1.Web/aspire1.Web.csproj';

const STARTUP_TIMEOUT = 30000; // 30 seconds
const HEALTH_CHECK_INTERVAL = 500; // 500ms

let serviceProcess: any = null;
let webProcess: any = null;

// Track if we started the processes (vs. they were already running)
let didStartService = false;
let didStartWeb = false;

/**
 * Check if the WeatherService is healthy by making a request to its health endpoint
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
 * Check if the Web Frontend is responding
 */
async function isWebHealthy(): Promise<boolean> {
  try {
    const response = await fetch(`${WEB_URL}/`);
    return response.ok || response.status === 404; // 404 is OK, means it's running but endpoint doesn't exist
  } catch (error) {
    return false;
  }
}

/**
 * Wait for the WeatherService to become healthy
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
 * Wait for the Web Frontend to become healthy
 */
async function waitForWebHealth(): Promise<void> {
  const startTime = Date.now();

  while (Date.now() - startTime < STARTUP_TIMEOUT) {
    if (await isWebHealthy()) {
      console.log(`✓ Web Frontend is healthy at ${WEB_URL}`);
      return;
    }
    await new Promise((resolve) => setTimeout(resolve, HEALTH_CHECK_INTERVAL));
  }

  throw new Error(
    `Web Frontend did not become healthy within ${STARTUP_TIMEOUT}ms. ` +
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
 * Start the Web Frontend
 */
async function startWeb(): Promise<void> {
  console.log(`Starting Web Frontend on ${WEB_URL}...`);

  return new Promise((resolve, reject) => {
    const env = {
      ...process.env,
      ASPNETCORE_URLS: WEB_URL,
      // Point the web frontend to the WeatherService API
      services__weatherservice__http__0: SERVICE_URL,
    };

    webProcess = spawn('dotnet', ['run', '--project', WEB_PROJECT, '--no-launch-profile', '--no-build'], {
      cwd: process.cwd(),
      env,
      stdio: ['pipe', 'pipe', 'pipe'],
    });

    let startupErrorOutput = '';

    webProcess.stderr?.on('data', (data: Buffer) => {
      const output = data.toString();
      startupErrorOutput += output;
      if (output.includes('Now listening on:') || output.includes('Application started')) {
        console.log(`[Web Frontend] ${output.trim()}`);
      }
    });

    webProcess.stdout?.on('data', (data: Buffer) => {
      const output = data.toString();
      if (output.includes('Now listening on:') || output.includes('Application started')) {
        console.log(`[Web Frontend] ${output.trim()}`);
      }
    });

    webProcess.on('error', (error: Error) => {
      reject(new Error(`Failed to start Web Frontend: ${error.message}`));
    });

    webProcess.on('exit', (code: number) => {
      if (code !== 0 && code !== null) {
        reject(
          new Error(
            `Web Frontend exited with code ${code}. ` +
            `Error output:\n${startupErrorOutput}`
          )
        );
      }
    });

    // Give the process a moment to start, then check health
    setTimeout(() => {
      waitForWebHealth()
        .then(resolve)
        .catch(reject);
    }, 1000);
  });
}

/**
 * Global setup: ensure both services are running before tests start
 */
async function globalSetup(): Promise<void> {
  console.log('\n🚀 Playwright Global Setup Started\n');

  try {
    // Check if WeatherService is already running
    const serviceHealthy = await isServiceHealthy();
    
    if (serviceHealthy) {
      console.log(`✓ WeatherService is already running at ${SERVICE_URL}`);
    } else {
      console.log(`✗ WeatherService is not responding at ${SERVICE_URL}`);
      await startService();
      didStartService = true;
    }

    // Check if Web Frontend is already running
    const webHealthy = await isWebHealthy();
    
    if (webHealthy) {
      console.log(`✓ Web Frontend is already running at ${WEB_URL}`);
    } else {
      console.log(`✗ Web Frontend is not responding at ${WEB_URL}`);
      await startWeb();
      didStartWeb = true;
    }

    console.log('\n✅ All services are ready!\n');
    
    // Write PIDs to file for teardown to read
    const pids = {
      servicePid: didStartService ? serviceProcess?.pid : null,
      webPid: didStartWeb ? webProcess?.pid : null,
    };
    fs.writeFileSync(PID_FILE, JSON.stringify(pids));
  } catch (error) {
    console.error('\n❌ Global Setup Failed:', error);
    
    // Clean up any started processes
    if (serviceProcess) {
      serviceProcess.kill();
    }
    if (webProcess) {
      webProcess.kill();
    }
    
    throw error;
  }
}

/**
 * Global teardown: cleanup services we started
 */
async function globalTeardown(): Promise<void> {
  console.log('\n🧹 Playwright Global Teardown Started\n');

  const killService = process.env.PLAYWRIGHT_KILL_SERVICE !== 'false';

  if (!killService) {
    console.log('⏩ Skipping service cleanup (PLAYWRIGHT_KILL_SERVICE=false)');
    return;
  }

  // Read PIDs from file
  let pids: { servicePid: number | null; webPid: number | null } = { servicePid: null, webPid: null };
  
  try {
    if (fs.existsSync(PID_FILE)) {
      const pidData = fs.readFileSync(PID_FILE, 'utf8');
      pids = JSON.parse(pidData);
      console.log(`📋 Found PIDs: service=${pids.servicePid}, web=${pids.webPid}`);
    } else {
      console.log('⚠️  No PID file found - services may not have been started by setup');
      return;
    }
  } catch (err) {
    console.log(`⚠️  Error reading PID file: ${err}`);
    return;
  }

  // Kill the WeatherService
  if (pids.servicePid) {
    console.log(`🛑 Stopping WeatherService (PID: ${pids.servicePid})...`);
    try {
      // Use pkill to kill all child processes first
      await execAsync(`pkill -P ${pids.servicePid}`).catch(() => {});
      // Then kill the parent
      process.kill(pids.servicePid, 'SIGTERM');
      await new Promise(resolve => setTimeout(resolve, 500));
      
      // Force kill if still running
      try {
        process.kill(pids.servicePid, 0); // Check if process exists
        process.kill(pids.servicePid, 'SIGKILL');
      } catch {
        // Process already dead, that's fine
      }
    } catch (err: any) {
      if (err.code !== 'ESRCH') { // ESRCH = no such process (already dead)
        console.log(`   Warning: Could not kill service process: ${err.message}`);
      }
    }
  }

  // Kill the Web Frontend
  if (pids.webPid) {
    console.log(`🛑 Stopping Web Frontend (PID: ${pids.webPid})...`);
    try {
      // Use pkill to kill all child processes first
      await execAsync(`pkill -P ${pids.webPid}`).catch(() => {});
      // Then kill the parent
      process.kill(pids.webPid, 'SIGTERM');
      await new Promise(resolve => setTimeout(resolve, 500));
      
      // Force kill if still running
      try {
        process.kill(pids.webPid, 0); // Check if process exists
        process.kill(pids.webPid, 'SIGKILL');
      } catch {
        // Process already dead, that's fine
      }
    } catch (err: any) {
      if (err.code !== 'ESRCH') { // ESRCH = no such process (already dead)
        console.log(`   Warning: Could not kill web process: ${err.message}`);
      }
    }
  }

  // Clean up PID file
  try {
    if (fs.existsSync(PID_FILE)) {
      fs.unlinkSync(PID_FILE);
    }
  } catch (err) {
    console.log(`   Warning: Could not remove PID file: ${err}`);
  }

  // Give processes time to shut down gracefully
  await new Promise(resolve => setTimeout(resolve, 1000));

  console.log('✅ Cleanup complete\n');
}

export default globalSetup;
export { globalTeardown };
