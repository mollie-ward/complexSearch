import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright configuration for E2E testing
 * 
 * Test coverage:
 * - Desktop Chrome
 * - Desktop Firefox
 * - Mobile (iPhone 12)
 * 
 * Features:
 * - Parallel execution
 * - CI/CD integration (2 retries on failure)
 * - HTML reporter
 * - Screenshot on failure
 * - Trace on first retry
 * - Auto-start dev server
 */
export default defineConfig({
  // Test directory
  testDir: './tests/e2e',
  
  // Run tests in parallel
  fullyParallel: true,
  
  // Fail the build on CI if you accidentally left test.only in the source code
  forbidOnly: !!process.env.CI,
  
  // Retry on CI only
  retries: process.env.CI ? 2 : 0,
  
  // Opt out of parallel tests on CI
  workers: process.env.CI ? 1 : undefined,
  
  // Reporter to use
  reporter: [
    ['html', { outputFolder: 'playwright-report' }],
    ['list'],
    ...(process.env.CI ? [['github'] as const] : []),
  ],
  
  // Shared settings for all tests
  use: {
    // Base URL to use in actions like `await page.goto('/')`
    baseURL: 'http://localhost:3000',
    
    // Collect trace when retrying the failed test
    trace: 'on-first-retry',
    
    // Capture screenshot on failure
    screenshot: 'only-on-failure',
    
    // Video capture on failure
    video: 'retain-on-failure',
    
    // Maximum time each action such as `click()` can take
    actionTimeout: 10000,
  },
  
  // Configure projects for major browsers
  projects: [
    {
      name: 'chromium',
      use: { 
        ...devices['Desktop Chrome'],
        // Browser viewport size
        viewport: { width: 1280, height: 720 },
      },
    },
    {
      name: 'firefox',
      use: { 
        ...devices['Desktop Firefox'],
        viewport: { width: 1280, height: 720 },
      },
    },
    {
      name: 'mobile',
      use: { 
        ...devices['iPhone 12'],
      },
    },
  ],
  
  // Run your local dev server before starting the tests
  webServer: {
    command: 'cd frontend && npm run dev',
    url: 'http://localhost:3000',
    reuseExistingServer: !process.env.CI,
    timeout: 120 * 1000, // 2 minutes
    stdout: 'pipe',
    stderr: 'pipe',
  },
  
  // Global timeout for each test
  timeout: 30 * 1000, // 30 seconds
  
  // Expect timeout for assertions
  expect: {
    timeout: 5000, // 5 seconds
  },
});
