import { test, expect } from '@playwright/test';
import { mkdtempSync, writeFileSync, appendFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { ServerLogReader } from '../src/evidence/server-log-reader.js';

/**
 * DEF-ENV-004 — the server-error invariant against the authoritative log format.
 *
 * These run without a browser: the unit under test is a file parser, and the assertion it feeds
 * (expectNoUnexpectedServerErrors) is the one the platform exists for. Every case below is a shape
 * taken from a real collected ComplianceService log.
 */

/** Writes a baseline, marks it, then appends `appended` — mirroring how a step is measured. */
function readErrorsFor(appended: string) {
  const dir = mkdtempSync(join(tmpdir(), 'rv-serverlog-'));
  const file = join(dir, 'ComplianceService.log');
  writeFileSync(file, 'baseline line that predates the step\n', 'utf8');

  const reader = new ServerLogReader(dir);
  reader.mark();

  appendFileSync(file, appended, 'utf8');
  return reader.errorsSince();
}

const clef = (fields: Record<string, unknown>) =>
  JSON.stringify({ '@t': '2026-08-12T05:12:36.7847652Z', ...fields }) + '\n';

test.describe('ServerLogReader.errorsSince — CLEF (authoritative runtime format)', () => {
  test('informational entries carry no @l and are not errors', () => {
    // CLEF omits the level for Information; 1,624 of 2,843 real lines had no @l.
    expect(readErrorsFor(clef({ '@m': 'HTTP "GET" "/health" responded 200 in 1.2 ms' }))).toHaveLength(0);
  });

  test('warnings are not errors', () => {
    expect(readErrorsFor(clef({ '@l': 'Warning', '@m': 'No hospitals found with compliance programs.' }))).toHaveLength(0);
  });

  test('Error is detected', () => {
    const errors = readErrorsFor(
      clef({ '@l': 'Error', '@m': 'HTTP "GET" "/api/v1/findings" responded 503 in 2.1 ms', StatusCode: 503 }),
    );
    expect(errors).toHaveLength(1);
    expect(errors[0]!.level).toBe('ERR');
    expect(errors[0]!.service).toBe('ComplianceService');
  });

  test('Fatal is detected', () => {
    const errors = readErrorsFor(clef({ '@l': 'Fatal', '@m': 'Unhandled domain exception in ComplianceService.' }));
    expect(errors).toHaveLength(1);
    expect(errors[0]!.level).toBe('FTL');
  });

  test('a message merely containing [ERR] is not an error', () => {
    // The regression strict parsing exists to prevent: text is not level.
    expect(readErrorsFor(clef({ '@m': 'GET "/api/v1/search?q=%5BERR%5D" responded 200' }))).toHaveLength(0);
  });

  test('documented environment noise is still filtered in CLEF form', () => {
    expect(readErrorsFor(clef({ '@l': 'Error', '@m': 'GET "/api/v1/notifications/unread-count" responded 502' }))).toHaveLength(0);
  });
});

test.describe('ServerLogReader.errorsSince — console format (developer-started stack)', () => {
  test('[ERR] is still detected', () => {
    const errors = readErrorsFor('[05:12:36 ERR] ComplianceService failed to resolve DashboardSnapshotService\n');
    expect(errors).toHaveLength(1);
    expect(errors[0]!.level).toBe('ERR');
  });

  test('[FTL] is still detected', () => {
    expect(readErrorsFor('[05:12:36 FTL] host terminated unexpectedly\n')[0]!.level).toBe('FTL');
  });

  test('[WRN] and [INF] are not errors', () => {
    expect(readErrorsFor('[05:12:36 WRN] slow query\n[05:12:37 INF] request finished\n')).toHaveLength(0);
  });
});

test.describe('ServerLogReader.errorsSince — mixed and malformed content', () => {
  test('build output and blank lines are ignored', () => {
    const build =
      'ComplianceService.csproj : warning NU1903: Package has a known vulnerability\n' +
      '  Build succeeded.\n\n' +
      'Using launch settings from ...launchSettings.json...\n';
    expect(readErrorsFor(build)).toHaveLength(0);
  });

  test('a truncated JSON line does not throw and is not an error', () => {
    expect(readErrorsFor('{"@t":"2026-08-12T05:12:36.78","@l":"Err\n')).toHaveLength(0);
  });

  test('mixed build, informational, warning, console and CLEF content yields only the real failures', () => {
    const mixed =
      '  Build succeeded.\n' +
      clef({ '@m': 'HTTP "GET" "/health" responded 200 in 1.1 ms' }) +
      clef({ '@l': 'Warning', '@m': 'No store type was specified for the decimal property' }) +
      clef({ '@l': 'Error', '@m': 'HTTP "GET" "/api/v1/findings" responded 503 in 2.1 ms' }) +
      '[05:12:38 INF] request finished\n' +
      '[05:12:39 ERR] projection failed\n' +
      clef({ '@l': 'Fatal', '@m': 'host terminated' });

    const errors = readErrorsFor(mixed);
    expect(errors.map(e => e.level)).toEqual(['ERR', 'ERR', 'FTL']);
  });
});
