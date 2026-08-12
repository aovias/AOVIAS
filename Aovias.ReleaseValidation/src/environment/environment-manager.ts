import { spawn, spawnSync } from 'node:child_process';
import {
  appendFileSync,
  closeSync,
  existsSync,
  mkdirSync,
  openSync,
  readdirSync,
  readSync,
  statSync,
} from 'node:fs';
import { join, resolve } from 'node:path';
import { APP_BASE_URL, ARTIFACT_ROOT, SERVICES, type ServiceDefinition } from './service-registry.js';
import { probeAllServices, probeApplication, probeDatabase, type ProbeResult } from './probes.js';

export const REPO_ROOT = resolve(process.cwd(), '..');

export interface EnvironmentReadiness {
  readonly ready: boolean;
  readonly checkedAt: string;
  readonly results: readonly ProbeResult[];
  readonly blockers: readonly string[];
  readonly remedies: readonly string[];
}

/**
 * Owns the runtime the certification platform executes against.
 *
 * Deployment *rules* belong to Aovias.DeploymentValidation and are not reimplemented here; this
 * class answers a narrower question — "can a scenario start right now, and if not, exactly why".
 * RC2 repeatedly began work against a stack that looked up and was not, so readiness is reported as
 * blockers plus remedies rather than a boolean.
 */
export class EnvironmentManager {
  private readonly logDir: string;
  /** Byte offset already collected from each authoritative runtime log (DEF-ENV-001). */
  private readonly runtimeOffsets = new Map<string, number>();

  constructor(
    private readonly runId: string,
    logDirectory?: string,
  ) {
    this.logDir = logDirectory ?? join(ARTIFACT_ROOT, runId, 'server-logs');
    mkdirSync(this.logDir, { recursive: true });
  }

  get serverLogDirectory(): string {
    return this.logDir;
  }

  /** Full readiness assessment. Never throws — an unreachable environment is a result, not an error. */
  async assess(): Promise<EnvironmentReadiness> {
    const [services, app, db] = await Promise.all([
      probeAllServices(),
      probeApplication(APP_BASE_URL),
      probeDatabase(),
    ]);

    const results = [...services, app, db.connectivity, db.migrations];
    const blockers: string[] = [];
    const remedies: string[] = [];

    for (const r of results) {
      if (r.state === 'ok') continue;
      blockers.push(`${r.target}: ${r.detail}`);

      if (r.state === 'wrong-port') {
        // The detail already carries the exact command; surface it as the remedy verbatim.
        remedies.push(r.detail.split('Restart with: ')[1] ?? r.detail);
      } else if (r.target === 'Angular dev server') {
        remedies.push('Start the Angular dev server: npm start (WebApp)');
      } else if (r.target === 'SQL Server') {
        remedies.push('Start SQL Server Express (service MSSQL$SQLEXPRESS)');
      } else if (r.target === 'EF migrations') {
        remedies.push('Apply migrations: dotnet ef database update (ComplianceService)');
      } else {
        const svc = SERVICES.find(s => s.name === r.target);
        if (svc) remedies.push(this.startCommand(svc));
      }
    }

    return {
      ready: blockers.length === 0,
      checkedAt: new Date().toISOString(),
      results,
      blockers,
      remedies,
    };
  }

  /**
   * Starts every service that is not already healthy, capturing stdout and stderr to disk.
   *
   * Log capture is not optional. DEF-M5-007 failed behind an HTTP 200 and was invisible to the
   * browser, the dashboards and the API; it existed only in stdout. Every service this platform
   * starts is therefore started with its output retained.
   */
  async start(): Promise<EnvironmentReadiness> {
    const before = await probeAllServices();
    const toStart = SERVICES.filter(s => before.find(r => r.target === s.name)?.state !== 'ok');

    this.buildSequentially(toStart);

    for (const service of toStart) {
      this.spawnService(service);
    }

    if (toStart.length > 0) {
      await this.waitForServices(180_000);
    }

    // Bring the managed logs level with the running services, so a later collection yields only
    // what happened after startup rather than replaying the whole day's log.
    this.collectRuntimeLogs();

    return this.assess();
  }

  /**
   * DEF-ENV-001 — copies each service's authoritative runtime log into the managed server-log
   * directory, appending only what is new since the previous call.
   *
   * The services do not write their operational stream to stdout in any form this platform
   * receives: `<Service>.log` held 2,362 bytes of launcher and build output and did not move while
   * ComplianceService answered 600 requests. The output was never lost — Serilog writes it to
   * `ComplaincePlatform/logs/<Service>/<service>-<date>.clef`, which grew to 1,028,903 bytes over
   * the same window. The defect was ownership: the platform was reading a file the application does
   * not log to.
   *
   * Collection is offset-based and pull-driven rather than a tail process, because that is what the
   * evidence lifecycle already expects — ServerLogReader marks `<Service>.log` at the start of a
   * step and reports the delta at the end, so the managed file only has to be current at those two
   * moments. Rotation is handled by collecting every .clef in the directory against its own offset.
   */
  collectRuntimeLogs(): void {
    for (const service of SERVICES) {
      const runtimeDir = join(REPO_ROOT, 'ComplaincePlatform', 'logs', service.name);
      if (!existsSync(runtimeDir)) continue;

      const target = join(this.logDir, `${service.name}.log`);

      for (const entry of readdirSync(runtimeDir).filter(f => f.endsWith('.clef')).sort()) {
        const source = join(runtimeDir, entry);
        const size = statSync(source).size;
        const from = this.runtimeOffsets.get(source) ?? 0;

        // A shrinking file means Serilog rolled it; restart from the beginning of the new content.
        const start = size < from ? 0 : from;
        if (size <= start) continue;

        const fd = openSync(source, 'r');
        try {
          const buffer = Buffer.alloc(size - start);
          readSync(fd, buffer, 0, buffer.length, start);
          appendFileSync(target, buffer);
        } finally {
          closeSync(fd);
        }

        this.runtimeOffsets.set(source, size);
      }
    }
  }

  private startCommand(service: ServiceDefinition): string {
    const profile = service.launchProfile ? `--launch-profile ${service.launchProfile} ` : '';
    return `dotnet run ${profile}--project ${service.project}`;
  }

  /**
   * DEF-ENV-002 — compiles the services one at a time, before any of them is started.
   *
   * `dotnet run` builds before it runs, and the six services do not own disjoint build graphs:
   * Shared.Infrastructure and Shared.Logging are referenced by all six, Shared.Authorization by
   * three. Starting them together therefore started six independent MSBuild processes writing the
   * same obj/Debug/net10.0 directories, which do not coordinate across processes. They raced on
   *   Shared.Authorization.csproj.FileListAbsolute.txt ... being used by another process
   * and five of the six services failed to build — the environment came up with one service.
   *
   * Sequential builds remove the race at its source. The services are still started concurrently
   * below, but with --no-build, so no MSBuild work happens at start time at all.
   */
  private buildSequentially(services: readonly ServiceDefinition[]): void {
    for (const service of services) {
      const outFd = openSync(join(this.logDir, `${service.name}.log`), 'a');
      const errFd = openSync(join(this.logDir, `${service.name}.err.log`), 'a');
      try {
        spawnSync('dotnet', ['build', join(REPO_ROOT, service.project)], {
          cwd: REPO_ROOT,
          stdio: ['ignore', outFd, errFd],
        });
      } finally {
        closeSync(outFd);
        closeSync(errFd);
      }
    }
  }

  private spawnService(service: ServiceDefinition): void {
    const args = ['run'];
    if (service.launchProfile) args.push('--launch-profile', service.launchProfile);
    // Already compiled by buildSequentially; building again here would reintroduce the race.
    args.push('--no-build');
    args.push('--project', join(REPO_ROOT, service.project));

    // Give the child its own file descriptors rather than piping through this process.
    //
    // Piping kept 12 Socket handles referenced in the parent (6 services × stdout+stderr), which is
    // why the CLI printed "Environment Ready" and then never exited — measured directly:
    //   ACTIVE_HANDLES={"Socket":13,"ChildProcess":1}  ACTIVE_REQUESTS=0
    //
    // It was also wrong on its own terms: these children are detached and outlive the CLI, so a
    // parent-owned pipe would have stopped capturing the moment the CLI exited. Handing the child
    // real fds means the service writes its own log for as long as it runs, and the parent holds
    // nothing.
    const outFd = openSync(join(this.logDir, `${service.name}.log`), 'a');
    const errFd = openSync(join(this.logDir, `${service.name}.err.log`), 'a');

    const child = spawn('dotnet', args, {
      cwd: REPO_ROOT,
      detached: true,
      stdio: ['ignore', outFd, errFd],
    });

    // The child holds its own duplicates; this process must not keep them open.
    closeSync(outFd);
    closeSync(errFd);
    child.unref();
  }

  private async waitForServices(timeoutMs: number): Promise<void> {
    const deadline = Date.now() + timeoutMs;
    while (Date.now() < deadline) {
      const results = await probeAllServices();
      if (results.every(r => r.state === 'ok')) return;
      await new Promise(r => setTimeout(r, 3_000));
    }
  }

  /** Human-readable readiness report for the certification artifact and for CI logs. */
  static format(readiness: EnvironmentReadiness): string {
    const lines: string[] = [];
    lines.push(readiness.ready ? 'Environment Ready' : 'Environment Not Ready');
    lines.push(`Checked at ${readiness.checkedAt}`);
    lines.push('');

    for (const r of readiness.results) {
      const mark = r.state === 'ok' ? 'ok  ' : r.state === 'wrong-port' ? 'PORT' : 'FAIL';
      lines.push(`  [${mark}] ${r.target.padEnd(26)} ${r.detail}`);
    }

    if (!readiness.ready) {
      lines.push('', 'Blockers:');
      readiness.blockers.forEach(b => lines.push(`  - ${b}`));
      lines.push('', 'Remedies:');
      [...new Set(readiness.remedies)].forEach(r => lines.push(`  $ ${r}`));
    }
    return lines.join('\n');
  }
}
