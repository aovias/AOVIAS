import { EnvironmentManager } from './environment-manager.js';

/**
 * `npm run env:check` — answers "Environment Ready" or "Environment Not Ready" with precise
 * diagnostics and the exact commands to fix it. Exit code 0 / 1 so CI can gate on it.
 */
const runId = process.env['RV_RUN_ID'] ?? new Date().toISOString().replace(/[:.]/g, '-');
const manager = new EnvironmentManager(runId);

// DEF-ENV-001 — pulls the running services' authoritative runtime logs into the managed
// server-log directory without starting or stopping anything.
if (process.argv.includes('--collect-logs')) {
  manager.collectRuntimeLogs();
  console.log(`Runtime logs collected into ${manager.serverLogDirectory}`);
  process.exitCode = 0;
} else {

const shouldStart = process.argv.includes('--start');
const readiness = shouldStart ? await manager.start() : await manager.assess();

console.log(EnvironmentManager.format(readiness));

// Set exitCode rather than calling process.exit(): forcing exit while sockets from the health
// probes were still closing produced a libuv assertion and an exit code of 127, which CI would
// have read as "environment check crashed" on a perfectly ready environment.
process.exitCode = readiness.ready ? 0 : 1;
}
