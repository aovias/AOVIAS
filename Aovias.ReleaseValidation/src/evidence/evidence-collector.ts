import { mkdirSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import type { ConsoleMessage, Page, Request, TestInfo } from '@playwright/test';
import { ARTIFACT_ROOT } from '../environment/service-registry.js';
import { ServerLogReader } from './server-log-reader.js';

export interface NetworkFailure {
  readonly url: string;
  readonly method: string;
  readonly status: number | 'aborted';
  readonly body?: string;
}

/**
 * Gathers everything needed to diagnose a failure without re-running the scenario.
 *
 * Trace, video and screenshots come from Playwright. Console, network failures and — critically —
 * server logs are collected here, because Playwright cannot see the server and RC2 showed that is
 * where the hardest defects live.
 */
export class EvidenceCollector {
  readonly consoleErrors: string[] = [];
  readonly networkFailures: NetworkFailure[] = [];
  private readonly logReader: ServerLogReader;

  constructor(
    private readonly page: Page,
    serverLogDirectory: string,
    private readonly runId: string,
  ) {
    this.logReader = new ServerLogReader(serverLogDirectory);
    this.attach();
  }

  /** Begins a step: server-log offsets are marked so later reads are scoped to this step. */
  beginStep(): void {
    this.logReader.mark();
  }

  serverErrors() {
    return this.logReader.errorsSince();
  }

  serverLogSlice(): string {
    return this.logReader.since();
  }

  private attach(): void {
    this.page.on('console', (msg: ConsoleMessage) => {
      if (msg.type() === 'error') this.consoleErrors.push(msg.text());
    });

    this.page.on('requestfailed', (req: Request) => {
      this.networkFailures.push({ url: req.url(), method: req.method(), status: 'aborted' });
    });

    this.page.on('response', async res => {
      if (res.status() < 400) return;
      // Response bodies on failures are the difference between "400 Bad Request" and the sentence
      // that names the defect — RC2's deadlock was diagnosed entirely from one.
      let body: string | undefined;
      try {
        body = (await res.text()).slice(0, 2_000);
      } catch {
        /* body already consumed or streamed */
      }
      this.networkFailures.push({
        url: res.url(),
        method: res.request().method(),
        status: res.status(),
        ...(body ? { body } : {}),
      });
    });
  }

  /** Writes all non-Playwright evidence and attaches it to the test report. */
  async persist(testInfo: TestInfo, stepName = 'scenario'): Promise<void> {
    const dir = join(ARTIFACT_ROOT, this.runId, sanitise(testInfo.title), sanitise(stepName));
    mkdirSync(dir, { recursive: true });

    const files: Record<string, string> = {
      'console.log': this.consoleErrors.join('\n'),
      'network-failures.json': JSON.stringify(this.networkFailures, null, 2),
      'server-logs.log': this.serverLogSlice(),
      'server-errors.json': JSON.stringify(this.serverErrors(), null, 2),
    };

    for (const [name, content] of Object.entries(files)) {
      if (!content.trim() || content === '[]') continue;
      const path = join(dir, name);
      writeFileSync(path, content, 'utf8');
      await testInfo.attach(name, { path });
    }
  }
}

function sanitise(value: string): string {
  return value.replace(/[^a-z0-9-_]+/gi, '-').slice(0, 80).toLowerCase();
}
