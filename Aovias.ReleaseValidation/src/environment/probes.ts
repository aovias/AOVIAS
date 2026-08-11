import { execFile } from 'node:child_process';
import { promisify } from 'node:util';
import { DATABASE, SERVICES, type ServiceDefinition } from './service-registry.js';

const exec = promisify(execFile);

export type ProbeState = 'ok' | 'down' | 'wrong-port' | 'unreachable';

export interface ProbeResult {
  readonly target: string;
  readonly state: ProbeState;
  readonly detail: string;
  readonly elapsedMs: number;
}

/**
 * Three services are served over HTTPS with ASP.NET development certificates. Node's `fetch`
 * rejects them, which made the first run of this probe report OrganizationService, PersonService and
 * AccessGovernanceService as unreachable while they were healthy and answering `curl -sk` with 200.
 *
 * That is a false negative in the harness, and the most damaging kind: a validation platform that
 * blames the product for its own defects is worse than no platform. The relaxation is scoped to this
 * validation process, which only ever talks to localhost dev services.
 */
process.env['NODE_TLS_REJECT_UNAUTHORIZED'] = '0';

const ALLOW_SELF_SIGNED = { ...process.env, NODE_TLS_REJECT_UNAUTHORIZED: '0' };

async function httpStatus(url: string, timeoutMs = 5_000): Promise<number | null> {
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), timeoutMs);
  try {
    const res = await fetch(url, { signal: controller.signal });
    return res.status;
  } catch {
    return null;
  } finally {
    clearTimeout(timer);
  }
}

/**
 * Probes one service and, when it is down, checks whether it is actually running on the port it
 * binds under the wrong launch profile. That distinction is the difference between "start the
 * service" and "restart it with --launch-profile https", which RC2 spent real time discovering.
 */
export async function probeService(service: ServiceDefinition): Promise<ProbeResult> {
  const started = Date.now();
  const status = await httpStatus(service.healthUrl);

  if (status === 200) {
    return { target: service.name, state: 'ok', detail: `${service.healthUrl} → 200`, elapsedMs: Date.now() - started };
  }

  for (const candidate of service.wrongPortSymptom ?? []) {
    if ((await httpStatus(candidate)) === 200) {
      return {
        target: service.name,
        state: 'wrong-port',
        detail:
          `Responding on ${candidate} but NOT on ${service.healthUrl}. ` +
          `Started without the '${service.launchProfile}' launch profile; the Gateway cannot reach it. ` +
          `Restart with: dotnet run --launch-profile ${service.launchProfile} --project ${service.project}`,
        elapsedMs: Date.now() - started,
      };
    }
  }

  return {
    target: service.name,
    state: 'down',
    detail: status === null ? `${service.healthUrl} unreachable` : `${service.healthUrl} → ${status}`,
    elapsedMs: Date.now() - started,
  };
}

export async function probeAllServices(): Promise<ProbeResult[]> {
  return Promise.all(SERVICES.map(probeService));
}

export async function probeApplication(baseUrl: string): Promise<ProbeResult> {
  const started = Date.now();
  const status = await httpStatus(baseUrl, 10_000);
  return {
    target: 'Angular dev server',
    state: status && status < 500 ? 'ok' : 'down',
    detail: status === null ? `${baseUrl} unreachable` : `${baseUrl} → ${status}`,
    elapsedMs: Date.now() - started,
  };
}

/**
 * SQL Server reachability and migration state.
 *
 * This is the one place the platform touches SQL, and it is read-only and structural: is the server
 * up, does the database exist, has EF applied migrations. Test *data* never comes from here — see
 * ARCHITECTURE.md §3.
 */
export async function probeDatabase(): Promise<{ connectivity: ProbeResult; migrations: ProbeResult }> {
  const started = Date.now();
  const query =
    "SET NOCOUNT ON; " +
    "SELECT 'db=' + DB_NAME() + ';migrations=' + " +
    "CONVERT(varchar(10), (SELECT COUNT(*) FROM sys.tables WHERE name = '__EFMigrationsHistory')) + " +
    "';applied=' + CONVERT(varchar(10), ISNULL((SELECT COUNT(*) FROM __EFMigrationsHistory), -1));";

  try {
    const { stdout } = await exec(
      'sqlcmd',
      ['-S', DATABASE.server, '-d', DATABASE.database, '-E', '-C', '-h', '-1', '-W', '-Q', query],
      { env: ALLOW_SELF_SIGNED, timeout: 20_000 },
    );

    const line = stdout.split('\n').map(l => l.trim()).find(l => l.startsWith('db='));
    if (!line) {
      return {
        connectivity: { target: 'SQL Server', state: 'unreachable', detail: `Unexpected sqlcmd output: ${stdout.slice(0, 160)}`, elapsedMs: Date.now() - started },
        migrations: { target: 'EF migrations', state: 'unreachable', detail: 'not determined', elapsedMs: 0 },
      };
    }

    const applied = Number(/applied=(-?\d+)/.exec(line)?.[1] ?? -1);
    return {
      connectivity: { target: 'SQL Server', state: 'ok', detail: `${DATABASE.server}/${DATABASE.database} reachable`, elapsedMs: Date.now() - started },
      migrations: {
        target: 'EF migrations',
        state: applied > 0 ? 'ok' : 'down',
        detail: applied > 0 ? `${applied} migrations applied` : 'no __EFMigrationsHistory rows — database not migrated',
        elapsedMs: 0,
      },
    };
  } catch (error) {
    return {
      connectivity: {
        target: 'SQL Server',
        state: 'unreachable',
        detail: `sqlcmd failed against ${DATABASE.server}: ${(error as Error).message.split('\n')[0]}`,
        elapsedMs: Date.now() - started,
      },
      migrations: { target: 'EF migrations', state: 'unreachable', detail: 'not determined', elapsedMs: 0 },
    };
  }
}
