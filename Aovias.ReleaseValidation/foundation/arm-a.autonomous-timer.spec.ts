import { test } from '../src/runtime.js';
import { expect } from '@playwright/test';
import { RUN_ID } from '../src/runtime.js';

/**
 * DEF-CTX-001.A — Arm A. Autonomous timer observation.
 *
 * Hypothesis: when persisted hospital/journey state is absent, SidebarComponent's 400 ms auto-popup
 * path creates the context dialog with no operator click.
 *
 * This spec deliberately does NOT request the `workspace` or `hospitalContext` fixtures. Playwright
 * instantiates fixtures lazily, so not naming them means the operators are never constructed —
 * `activationOperatorInvoked: false` is proven structurally rather than by counting clicks.
 *
 * No seeding. No activation affordance is touched. Observation only.
 */

const CREDENTIALS = {
  email: process.env['RV_USER'] ?? '',
  password: process.env['RV_PASSWORD'] ?? '',
};

/** Generous margin over the product's documented 400 ms timer. Observation window, not a product sleep. */
const TIMER_OBSERVATION_WINDOW_MS = 2_000;

test('DEF-CTX-001.A Arm A: autonomous dialog creation without operator action', async ({
  page,
  auth,
  environment,
  diagnostics,
}, testInfo) => {
  test.skip(!CREDENTIALS.email, 'RV_USER / RV_PASSWORD not provided');

  let validity: 'valid' | 'invalid' = 'valid';
  let invalidReason = '';

  // Validity gate: an unhealthy environment invalidates the run rather than failing it.
  if (!environment.ready) {
    validity = 'invalid';
    invalidReason = 'environment-not-ready';
  }

  let dialogCount = -1;
  let dialogVisible = false;
  let storage = { hospital: null as string | null, journey: null as string | null, framework: null as string | null };

  if (validity === 'valid') {
    try {
      await auth.login(CREDENTIALS);
      diagnostics.captureLifecycle('authenticated');

      // A route that renders the sidebar. No activation affordance is clicked here or anywhere.
      await page.goto('/app/audit/findings', { waitUntil: 'domcontentloaded' });
      diagnostics.captureLifecycle('navigated-sidebar-surface');

      // Confirm the surface carrying the timer is actually present; absence invalidates the run.
      const sidebarPresent = await page
        .getByTestId('profile-menu-trigger')
        .isVisible()
        .catch(() => false);

      if (!sidebarPresent) {
        validity = 'invalid';
        invalidReason = 'sidebar-surface-unavailable';
      } else {
        // Allow the existing 400 ms auto-popup timer its opportunity. Nothing is clicked.
        await page.waitForTimeout(TIMER_OBSERVATION_WINDOW_MS);
        diagnostics.captureLifecycle('timer-window-elapsed');

        const inventory = await diagnostics.captureOverlayInventory();
        if (!inventory.ok) {
          validity = 'invalid';
          invalidReason = 'overlay-inventory-uncapturable';
        } else {
          dialogCount = inventory.value.dialogCount;
          dialogVisible = inventory.value.dialogs.some(d => d.visible);
        }

        storage = await page.evaluate(() => ({
          hospital: localStorage.getItem('hospital_id'),
          journey: sessionStorage.getItem('selected_framework_journey_id'),
          framework: sessionStorage.getItem('selected_framework_id'),
        }));
      }
    } catch (error) {
      validity = 'invalid';
      invalidReason = `unrelated-failure: ${(error as Error).message.split('\n')[0]}`;
    }
  }

  const summary = {
    runId: RUN_ID,
    arm: 'A',
    validity,
    invalidReason,
    dialogCount,
    dialogVisible,
    hospitalPresent: !!storage.hospital,
    journeyPresent: !!storage.journey,
    frameworkPresent: !!storage.framework,
    activationOperatorInvoked: false, // structural: neither operator fixture is requested
    activationClickCount: 0,
    outcome: validity === 'invalid' ? 'invalid' : dialogCount >= 1 ? 'autonomous-dialog' : 'no-dialog',
  };

  await testInfo.attach('arm-a-summary.json', {
    contentType: 'application/json',
    body: JSON.stringify(summary, null, 2),
  });

  console.log(`ARMA ${JSON.stringify(summary)}`);

  // The measurement itself never fails the run; classification happens in analysis.
  expect(summary.arm).toBe('A');
});
