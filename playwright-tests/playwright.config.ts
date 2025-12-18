import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright configuration for TicketingASP E2E tests
 * Focused on essential security and functionality tests
 * @see https://playwright.dev/docs/test-configuration
 */

export default defineConfig({
  testDir: './tests',
  
  /* Only run the focused security tests */
  testMatch: ['**/core-security.spec.ts', '**/core-functionality.spec.ts'],
  
  /* Ignore setup/teardown files in regular test runs */
  testIgnore: ['**/global.setup.ts', '**/global.teardown.ts'],
  
  /* Run tests in files in parallel */
  fullyParallel: true,
  
  /* Fail the build on CI if you accidentally left test.only in the source code */
  forbidOnly: !!process.env.CI,
  
  /* Retry on CI only */
  retries: process.env.CI ? 2 : 0,
  
  /* Limit workers for stability */
  workers: process.env.CI ? 1 : 2,
  
  /* Reporter to use */
  reporter: [
    ['html', { open: 'never' }],
    ['list']
  ],

  /* Global setup and teardown */
  globalSetup: './global-setup.ts',
  globalTeardown: './global-teardown.ts',
  
  /* Shared settings for all the projects below */
  use: {
    /* Base URL for the application - use BASE_URL in CI, localhost for local dev */
    baseURL: process.env.BASE_URL || process.env.PLAYWRIGHT_BASE_URL || 'http://localhost:5050',

    /* Collect trace when retrying the failed test */
    trace: 'on-first-retry',

    /* Capture screenshot on failure */
    screenshot: 'only-on-failure',

    /* Record video on failure */
    video: 'on-first-retry',

    /* Set default timeout for actions */
    actionTimeout: 15000,

    /* Set navigation timeout */
    navigationTimeout: 45000,
  },

  /* Only run on Chromium for speed - security tests don't need cross-browser */
  projects: [
    {
      name: 'chromium',
      use: { 
        ...devices['Desktop Chrome'],
      },
    },
  ],

  /* Global timeout for each test - increased to handle slower operations */
  timeout: 90000,

  /* Expect timeout */
  expect: {
    timeout: 15000,
  },

  /* Run your local dev server before starting the tests - disabled in CI when using external URL */
  webServer: process.env.CI ? undefined : {
    command: 'dotnet run --project ../TicketingASP.csproj --urls http://localhost:5050',
    url: 'http://localhost:5050',
    reuseExistingServer: true,
    timeout: 120000,
    stdout: 'pipe',
    stderr: 'pipe',
  },
});
