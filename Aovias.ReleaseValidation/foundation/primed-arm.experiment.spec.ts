import { test } from '../src/runtime.js';
import { expect } from '@playwright/test';

/**
 * DEF-CTX-001.A — primed arm.
 *
 * Independent variable: presence of the Sidebar auto-popup guard's inputs.
 *
 *   sidebar.component.ts:1761   if (!autoPopupFired && (!persistedHospitalId || !persistedJourneyId))
 *
 * The guard is an OR over two sessionStorage keys and verifies no provenance, so seeding satisfies
 * the same predicate production evaluates. `selected_framework_id` is deliberately NOT seeded —
 * per ADR-014 the framework is derived, and seeding it would pre-empt the derivation, changing the
 * code path under test rather than isolating the variable.
 *
 * Control: archived Q1.1 baseline (13 × dialogCount 1, 7 × dialogCount 2).
 */

const CREDENTIALS = {
  email: process.env['RV_USER'] ?? '',
  password: process.env['RV_PASSWORD'] ?? '',
};

/** Real ids from the archived baseline bundles. */
const SEED = {
  hospitalId: 'd9f5554a-bf83-4262-b386-a31587b0ba06',
  journeyId: 'e1460468-76d0-418c-85bf-d65cdb31a1ea',
} as const;

const KEYS = {
  hospital: 'selected_hospital_id',
  journey: 'selected_framework_journey_id',
} as const;

test('DEF-CTX-001.A primed arm: guard satisfied before navigation', async ({
  page,
  auth,
  workspace,
  diagnostics,
}, testInfo) => {
  test.skip(!CREDENTIALS.email, 'RV_USER / RV_PASSWORD not provided');

  // ── Phase 1 — seed before any application code runs ───────────────────────────────────────
  await page.addInitScript(
    ({ keys, seed }: { keys: typeof KEYS; seed: typeof SEED }) => {
      sessionStorage.setItem(keys.hospital, seed.hospitalId);
      sessionStorage.setItem(keys.journey, seed.journeyId);
    },
    { keys: KEYS, seed: SEED },
  );
  diagnostics.captureLifecycle('init-script-registered');

  // A navigation is required for the init script to execute; the gate is checked immediately
  // after, before any application interaction.
  await page.goto('/login', { waitUntil: 'domcontentloaded' });

  // ── Phase 2 — validation gate ─────────────────────────────────────────────────────────────
  const seeded = await page.evaluate(
    (keys: typeof KEYS) => ({
      hospital: sessionStorage.getItem(keys.hospital),
      journey: sessionStorage.getItem(keys.journey),
      framework: sessionStorage.getItem('selected_framework_id'),
    }),
    KEYS,
  );
  diagnostics.captureLifecycle('validation-gate');

  const gatePassed = seeded.hospital === SEED.hospitalId && seeded.journey === SEED.journeyId;

  await testInfo.attach('experiment-arm.json', {
    contentType: 'application/json',
    body: JSON.stringify(
      {
        experimentArm: gatePassed ? 'primed' : 'INVALID',
        persistedHospitalPresent: !!seeded.hospital,
        persistedJourneyPresent: !!seeded.journey,
        persistedFrameworkPresent: !!seeded.framework,
        seeded,
      },
      null,
      2,
    ),
  });

  // An unsatisfied gate makes the run Invalid, not Failed — it must not enter the statistics.
  if (!gatePassed) {
    console.log(`ARM=INVALID reason=gate-unsatisfied seeded=${JSON.stringify(seeded)}`);
    test.skip(true, 'Validation gate unsatisfied — run is Invalid, excluded from statistics');
  }

  // ── Phase 3 — execute, identically to the baseline ────────────────────────────────────────
  await auth.login(CREDENTIALS);
  diagnostics.captureLifecycle('authenticated');

  const claim = await page.evaluate(() => localStorage.getItem('hospital_id'));

  await page.goto('/app/audit/findings', { waitUntil: 'domcontentloaded' });
  diagnostics.captureLifecycle('navigated');

  let outcome = 'success';
  let failure = '';
  try {
    await workspace.activateWorkspace();
  } catch (error) {
    outcome = 'failed';
    failure = (error as Error).message.split('\n')[0] ?? '';
  }
  diagnostics.captureLifecycle('activation-attempted');

  const inventory = await diagnostics.captureOverlayInventory();
  const dialogCount = inventory.ok ? inventory.value.dialogCount : -1;

  // Single machine-readable line per run; the archived bundle carries the full detail.
  console.log(
    `ARM=primed hospitalClaimPresent=${!!claim} dialogCount=${dialogCount} ` +
      `outcome=${outcome} failure=${failure}`,
  );

  expect(dialogCount, 'overlay inventory must be capturable').toBeGreaterThanOrEqual(0);
});
