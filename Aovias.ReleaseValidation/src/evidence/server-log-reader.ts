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

/** CLEF levels that mean "the server failed", per Serilog's LogEventLevel names. */
const CLEF_FAILURE_LEVELS: Readonly<Record<string, 'ERR' | 'FTL'>> = { Error: 'ERR', Fatal: 'FTL' };

/**
 * Classifies one collected log line, or returns null if it does not report a failure.
 *
 * DEF-ENV-004 — the collected log carries two formats and the invariant has to be truthful about
 * both. The authoritative runtime stream is CLEF (one JSON object per line), where the level lives
 * in `@l` and is *omitted entirely* for Information — 1,624 of 2,843 collected lines had no `@l` at
 * all. The console format (`[12:00:00 ERR] ...`) still arrives whenever RV_SERVER_LOG_DIR points at
 * a stack someone started themselves, which runtime.ts explicitly supports.
 *
 * CLEF is parsed rather than pattern-matched. A regex over JSON would report an error for any
 * request whose path or message merely contained the text `[ERR]` or `"@l":"Error"`, which is the
 * kind of false positive that would eventually get this assertion switched off.
 */
function classify(line: string): 'ERR' | 'FTL' | null {
  const trimmed = line.trim();
  if (!trimmed) return null;

  if (trimmed.startsWith('{')) {
    try {
      const event: unknown = JSON.parse(trimmed);
      if (event && typeof event === 'object') {
        const level = (event as Record<string, unknown>)['@l'];
        // No @l means Information: CLEF omits the level for its default.
        return typeof level === 'string' ? (CLEF_FAILURE_LEVELS[level] ?? null) : null;
      }
    } catch {
      // Not a complete JSON object — fall through and treat it as console text.
    }
  }

  // Serilog's console template is "[{Timestamp:HH:mm:ss} {Level:u3}] ...", so the level is inside
  // the leading bracket alongside the timestamp — "[05:12:36 ERR]". The original pattern here
  // required a bare "[ERR]" and therefore never matched the product's console output either; the
  // timestamp is optional below so both spellings are accepted. Anchoring to the start of the line
  // keeps a message that merely quotes "[ERR]" from being reported as one.
  const match = /^\[(?:[^\]]*\s)?(ERR|FTL)\]/.exec(trimmed);
  return match ? (match[1] as 'ERR' | 'FTL') : null;
}

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
          const level = classify(line);
          if (!level) continue;
          if (EXPECTED_NOISE.some(n => n.pattern.test(line))) continue;
          entries.push({ service, level, line: line.trim() });
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
