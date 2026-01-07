/**
 * Playwright Global Teardown
 * Cleans up processes started during globalSetup
 * Referenced from playwright.config.ts
 */

// Import the process references and cleanup function from setup
import { globalTeardown } from './playwright-setup';

/**
 * Playwright calls this after all tests complete.
 * Tears down the test environment by killing spawned processes.
 */
export default globalTeardown;
