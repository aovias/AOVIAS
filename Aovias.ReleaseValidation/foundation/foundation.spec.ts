import { test } from '../src/runtime.js';
import {
  expectHealthyEnvironment,
  expectLoggedIn,
  expectNoConsoleErrors,
  expectNoUnexpectedServerErrors,
} from '../src/assertions/foundation.js';

/**
 * Proves the foundation itself, not the product.
 *
 * If these five assertions cannot pass, no business scenario can be trusted — a scenario built on an
 * unverified foundation reports the foundation's faults as product defects, which is precisely how
 * RC2 produced one false defect report.
 *
 * Credentials come from the environment. The platform never embeds them, and the Test Data Manager
 * (RC3 W1.2) will generate them per worker.
 */
const CREDENTIALS = {
  email: process.env['RV_USER'] ?? '',
  password: process.env['RV_PASSWORD'] ?? '',
};

test.describe('Release Validation foundation', () => {
  test('environment contract: the stack is ready for scenarios', async ({ environment }) => {
    expectHealthyEnvironment(environment);
  });

  test('authentication contract: login yields a complete session', async ({
    environment,
    auth,
    evidence,
  }) => {
    expectHealthyEnvironment(environment);
    test.skip(!CREDENTIALS.email, 'RV_USER / RV_PASSWORD not provided');

    await auth.login(CREDENTIALS);

    await expectLoggedIn(auth);
    expectNoConsoleErrors(evidence);
    expectNoUnexpectedServerErrors(evidence);
  });

  test('context contract: hospital and framework can be selected', async ({
    environment,
    auth,
    workspace,
    page,
    evidence,
    diagnostics,
  }) => {
    expectHealthyEnvironment(environment);
    test.skip(!CREDENTIALS.email, 'RV_USER / RV_PASSWORD not provided');

    await auth.login(CREDENTIALS);

    // The dialog is raised by routes that require framework scope, not by login. The operator
    // discovers state rather than assuming navigation (contract §10), so the scenario is
    // responsible for being somewhere context is actually needed.
    await page.goto('/app/audit/findings', { waitUntil: 'domcontentloaded' });
    await workspace.activateWorkspace();

    await workspace.verifyOperationalReadiness();
    expectNoConsoleErrors(evidence);
    expectNoUnexpectedServerErrors(evidence);
  });
});
