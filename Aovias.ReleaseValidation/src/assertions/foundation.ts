import { expect } from '@playwright/test';
import type { EnvironmentReadiness } from '../environment/environment-manager.js';
import { EnvironmentManager } from '../environment/environment-manager.js';
import type { EvidenceCollector } from '../evidence/evidence-collector.js';
import type { AuthOperator } from '../operator/auth.operator.js';
import type { ContextOperator } from '../operator/context.operator.js';

/**
 * The five foundational assertions. Deliberately not business assertions — those are grown from
 * Scenario 01's real needs rather than speculated (ARCHITECTURE.md §10, implementation order).
 *
 * Every one of these is a *contract*, executable rather than documented, per the workstream brief.
 */

/** Environment Contract — ready means services healthy, database reachable, migrations applied. */
export function expectHealthyEnvironment(readiness: EnvironmentReadiness): void {
  expect(
    readiness.ready,
    `Environment Not Ready\n\n${EnvironmentManager.format(readiness)}`,
  ).toBe(true);
}

/**
 * Authentication Contract — a successful login must produce an access token, a refresh token and a
 * user context, and must leave the login route.
 *
 * The refresh token is asserted because RC2 found a session could look valid while holding nothing
 * to renew with: the guard decided expiry locally and redirected without ever attempting renewal
 * (DEF-RC2S1-001).
 */
export async function expectLoggedIn(auth: AuthOperator): Promise<void> {
  const session = await auth.session();

  expect(session.hasAccessToken, 'Authentication Contract: no access token after login').toBe(true);
  expect(session.hasRefreshToken, 'Authentication Contract: no refresh token after login').toBe(true);
  expect(session.url, 'Authentication Contract: still on the login route after login').not.toContain('/login');
}

/** Context Contract — a hospital and framework must be selected before business work. */
export async function expectContextSelected(context: ContextOperator): Promise<void> {
  const current = await context.currentContext();

  expect(current.hospitalId, 'Context Contract: no hospital selected').toBeTruthy();
  expect(current.frameworkId, 'Context Contract: no framework selected').toBeTruthy();
}

/** No unhandled browser console errors during the step. */
export function expectNoConsoleErrors(evidence: EvidenceCollector): void {
  const errors = evidence.consoleErrors.filter(e => !isExpectedBrowserNoise(e));
  expect(errors, `Unexpected browser console errors:\n${errors.join('\n')}`).toHaveLength(0);
}

/**
 * The most important assertion in the platform.
 *
 * DEF-M5-007 threw "No service for type 'DashboardSnapshotService' has been registered" inside a
 * request that returned HTTP 200. The browser, the API and every dashboard reported success. Only
 * stdout disagreed. Without this assertion the platform would have certified that defect as passing.
 */
export function expectNoUnexpectedServerErrors(evidence: EvidenceCollector): void {
  const errors = evidence.serverErrors();
  const detail = errors.map(e => `  [${e.service}] ${e.line}`).join('\n');
  expect(
    errors,
    `Server logged ${errors.length} unexpected error(s) during a step that appeared to succeed:\n${detail}`,
  ).toHaveLength(0);
}

/** Console noise that is environmental rather than defective; each needs a reason. */
function isExpectedBrowserNoise(message: string): boolean {
  return (
    // No notification service runs in the validation environment (OBS-M3-C).
    /notifications\/unread-count/i.test(message) ||
    /Failed to load resource.*502/i.test(message)
  );
}
