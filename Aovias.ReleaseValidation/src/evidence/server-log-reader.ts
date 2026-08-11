import { existsSync, readFileSync, statSync } from 'node:fs';
import { join } from 'node:path';
import { LOGGED_SERVICES } from '../environment/service-registry.js';

export interface ServerLogEntry {
  readonly service: string;
  readonly level: 'ERR' | 'WRN' | 'FTL';
  readonly line: string;
}

/**
 * Patterns that appear in healthy runs and must not fail a scenario.
 *
 * Each entry needs a reason. An allow-list that accumulates unexplained entries is how the
 * server-log invariant would quietly stop working — which is the specific failure this whole
 * component exists to prevent.
 */
const EXPECTED_NOISE: readonly { pattern: RegExp; reason: string }[] = [
  {
    pattern: /notifications\/unread-count/i,
    reason: 'No notification service runs in the validation environment; polling 502s are expected (OBS-M3-C).',
  },
  {
    pattern: /CommunicationService send failed/i,
    reason: 'Outbound comms are not configured in the validation environment.',
  },
  {
    pattern: /ExecutionContext\.FailClosed/i,
    reason: 'Background paths run without tenant scope and fail closed by design; warning only.',
  },
];

/**
 * Reads service stdout by byte offset so a scenario sees only what happened during it.
 *
 * DEF-M5-007 is the reason this exists: a dashboard projection threw
 * "No service for type 'DashboardSnapshotService'" while the HTTP response was 200. Nothing in the
 * browser, the API or the dashboards showed it. Only stdout did.
 */
export class ServerLogReader {
  private readonly offsets = new Map<string, number>();

  constructor(private readonly logDirectory: string) {}

  /** Records current file sizes. Call at the start of a scenario or step. */
  mark(): void {
    for (const service of LOGGED_SERVICES) {
      for (const file of this.filesFor(service)) {
        this.offsets.set(file, existsSync(file) ? statSync(file).size : 0);
      }
    }
  }

  /** Returns everything written since the last mark(). */
  since(): string {
    const chunks: string[] = [];
    for (const service of LOGGED_SERVICES) {
      for (const file of this.filesFor(service)) {
        if (!existsSync(file)) continue;
        const from = this.offsets.get(file) ?? 0;
        const content = readFileSync(file, 'utf8');
        const slice = content.slice(from);
        if (slice.trim()) chunks.push(`===== ${service} =====\n${slice}`);
      }
    }
    return chunks.join('\n');
  }

  /** Errors and fatals written since the last mark(), excluding documented environment noise. */
  errorsSince(): ServerLogEntry[] {
    const entries: ServerLogEntry[] = [];
    for (const service of LOGGED_SERVICES) {
      for (const file of this.filesFor(service)) {
        if (!existsSync(file)) continue;
        const from = this.offsets.get(file) ?? 0;
        const slice = readFileSync(file, 'utf8').slice(from);

        for (const line of slice.split('\n')) {
          const match = /\[(ERR|FTL)\]/.exec(line);
          if (!match) continue;
          if (EXPECTED_NOISE.some(n => n.pattern.test(line))) continue;
          entries.push({ service, level: match[1] as 'ERR' | 'FTL', line: line.trim() });
        }
      }
    }
    return entries;
  }

  static explainNoise(): string {
    return EXPECTED_NOISE.map(n => `  ${n.pattern.source} — ${n.reason}`).join('\n');
  }

  private filesFor(service: string): string[] {
    return [join(this.logDirectory, `${service}.log`), join(this.logDirectory, `${service}.err.log`)];
  }
}
