import { defineConfig, devices } from '@playwright/test';
import { APP_BASE_URL, ARTIFACT_ROOT } from './src/environment/service-registry.js';

/**
 * Evidence settings are deliberately generous.
 *
 * RC2's most expensive defects (the four stacked in the evidence-registration path) each cost a
 * restart to diagnose because the run that found them kept nothing. Capturing a trace and a HAR on
 * every run is cheap; re-running a fifteen-minute hospital journey to learn why it failed is not.
 */
export default defineConfig({
  testDir: '.',
  testMatch: ['foundation/**/*.spec.ts', 'scenarios/**/*.spec.ts', 'regression/**/*.spec.ts'],

  // A business assertion that only passes on the second attempt is a defect, not a flake.
  // Retries exist for infrastructure faults and are off locally by design.
  retries: process.env['CI'] ? 1 : 0,
  workers: process.env['CI'] ? 4 : 1,
  fullyParallel: false,
  forbidOnly: !!process.env['CI'],

  // A hospital journey is long. These are journey timeouts, not page timeouts.
  timeout: 5 * 60 * 1000,
  expect: { timeout: 15_000 },

  outputDir: `${ARTIFACT_ROOT}/test-results`,
  reporter: [
    ['list'],
    ['html', { outputFolder: `${ARTIFACT_ROOT}/html-report`, open: 'never' }],
    ['json', { outputFile: `${ARTIFACT_ROOT}/results.json` }],
  ],

  use: {
    baseURL: APP_BASE_URL,
    trace: 'retain-on-failure',
    video: 'retain-on-failure',
    screenshot: 'only-on-failure',
    actionTimeout: 20_000,
    navigationTimeout: 45_000,
    // HAR is attached per-context by BrowserRuntime so each scenario gets its own file.
    ignoreHTTPSErrors: true,
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'], viewport: { width: 1440, height: 900 } },
    },
  ],
});
