import { test as base } from '@playwright/test';
import { join } from 'node:path';
import { ARTIFACT_ROOT } from './environment/service-registry.js';
import { EnvironmentManager, type EnvironmentReadiness } from './environment/environment-manager.js';
import { EvidenceCollector } from './evidence/evidence-collector.js';
import { DiagnosticCollector } from './evidence/diagnostic-collector.js';
import { AuthOperator } from './operator/auth.operator.js';
import { ContextOperator } from './operator/context.operator.js';
import { WorkspaceActivationOperator } from './operator/workspace-activation.operator.js';

export const RUN_ID = process.env['RV_RUN_ID'] ?? new Date().toISOString().replace(/[:.]/g, '-');

/**
 * Where the Environment Manager writes service stdout. Set RV_SERVER_LOG_DIR when the stack was
 * started outside the platform (a developer's own `dotnet run`) so evidence still includes server
 * logs — the platform must never be blind to the server just because it did not start it.
 */
const SERVER_LOG_DIR =
  process.env['RV_SERVER_LOG_DIR'] ?? join(ARTIFACT_ROOT, RUN_ID, 'server-logs');

interface Fixtures {
  readonly environment: EnvironmentReadiness;
  readonly evidence: EvidenceCollector;
  readonly auth: AuthOperator;
  /** Named `hospitalContext`, not `context`: Playwright already owns `context` (BrowserContext). */
  readonly hospitalContext: ContextOperator;
  /** ADR-015 activation. Default from F1.5C; hospitalContext retained as fallback pending removal. */
  readonly workspace: WorkspaceActivationOperator;
  /** Runtime-owned diagnostics; flushed automatically in a finally (F1.5H). */
  readonly diagnostics: DiagnosticCollector;
}

/**
 * The single entry point every scenario imports.
 *
 * Environment readiness is a worker-scoped fixture: assessed once per worker, not once per test,
 * because probing six services on every test would dominate the run. Evidence is test-scoped and
 * always persisted — including on success, where a passing test with server errors is exactly the
 * case RC2 missed.
 */
export const test = base.extend<Fixtures, { workerEnvironment: EnvironmentReadiness }>({
  workerEnvironment: [
    async ({}, use) => {
      const manager = new EnvironmentManager(RUN_ID);
      const readiness = await manager.assess();
      // Report readiness once per worker so a failing run explains itself in the CI log.
      if (!readiness.ready) {
        console.error(EnvironmentManager.format(readiness));
      }
      await use(readiness);
    },
    { scope: 'worker' },
  ],

  environment: async ({ workerEnvironment }, use) => {
    await use(workerEnvironment);
  },

  evidence: async ({ page }, use, testInfo) => {
    const collector = new EvidenceCollector(page, SERVER_LOG_DIR, RUN_ID);
    collector.beginStep();

    await use(collector);

    // Persist regardless of outcome. A green test that logged server errors is a defect this
    // platform exists to surface, and its evidence must survive.
    await collector.persist(testInfo);
  },

  auth: async ({ page }, use) => {
    await use(new AuthOperator(page));
  },

  hospitalContext: async ({ page }, use) => {
    await use(new ContextOperator(page));
  },

  workspace: async ({ page }, use) => {
    await use(new WorkspaceActivationOperator(page));
  },

  /**
   * Diagnostic collection, owned by the runtime rather than by scenarios.
   *
   * The flush is in a `finally` so it runs whether the test passed, failed, or threw mid-step.
   * F1.5E is the reason: a diagnostic that only reported on success died on a click and produced
   * nothing, leaving an investigation unresolved after a full run. Scenarios call capture methods
   * only for semantic checkpoints; they never have to remember to flush.
   */
  diagnostics: async ({ page }, use, testInfo) => {
    const collector = new DiagnosticCollector(page);
    collector.captureLifecycle('test-started');

    try {
      await use(collector);
      collector.captureLifecycle('test-completed');
    } finally {
      collector.captureLifecycle('flush');
      // captureEvidence is itself failure-tolerant; a broken page must not mask the real failure.
      await collector.captureEvidence(testInfo, `diagnostic-${testInfo.status ?? 'unknown'}`);
    }
  },
});

export { expect } from '@playwright/test';
