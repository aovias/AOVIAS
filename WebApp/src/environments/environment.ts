const API_PATH = '/api/v1';
const DEV_ANGULAR_PORT = '4200';
const DEV_GATEWAY_PORT = '5000';

/**
 * Resolve the Gateway port relative to the browser's current origin.
 *
 * - Same-origin deploy (reverse proxy / domain): use the current port (empty string
 *   means default 80/443, which we honour by omitting the port in the URL).
 * - SSR / jsdom fallback: `DEV_GATEWAY_PORT`.
 */
function resolveGatewayPort(): string {
  if (typeof window === 'undefined') return DEV_GATEWAY_PORT;
  const currentPort = window.location.port;
  return currentPort || DEV_GATEWAY_PORT;
}

/**
 * True when the app is served by `ng serve` on the dev port (see `proxy.conf.json`).
 * API calls use a same-origin relative base so the dev server proxies to Gateway — no CORS.
 */
function isNgDevServer(): boolean {
  return typeof window !== 'undefined' && window.location?.port === DEV_ANGULAR_PORT;
}

/**
 * Compute the Gateway API base URL at module load time. Must match whatever scheme/host
 * the browser is using — mixed scheme would break cookies and trigger mixed-content errors.
 *
 * Order of precedence:
 *   1. `window.__PRAMAN_API_BASE_URL__` override (set from `index.html` for staging/prod swaps
 *      without a rebuild).
 *   2. Relative `/api/v1` when on the Angular dev server (port 4200) — proxied to Gateway.
 *   3. Derived from `window.location.{protocol,hostname,port}` for LAN / same-origin deploys.
 *   4. SSR / jsdom fallback to `http://localhost:5000/api/v1`.
 */
function resolveApiBaseUrl(): string {
  const override = (globalThis as { __PRAMAN_API_BASE_URL__?: string }).__PRAMAN_API_BASE_URL__;
  if (override) return override;

  if (isNgDevServer()) {
    return API_PATH;
  }

  if (typeof window === 'undefined' || !window.location?.hostname) {
    return `http://localhost:${DEV_GATEWAY_PORT}${API_PATH}`;
  }

  const protocol = window.location.protocol; // 'http:' or 'https:' — avoids mixed-content
  const host = window.location.hostname || 'localhost';
  const port = resolveGatewayPort();
  const portPart = port ? `:${port}` : ''; // same-origin on 80/443 -> no explicit port
  return `${protocol}//${host}${portPart}${API_PATH}`;
}

export const environment = {
  production: false,
  /**
   * On `ng serve` (port 4200): relative `/api/v1` via `proxy.conf.json` → Gateway (no CORS).
   * Otherwise resolved from `window.location` for LAN / reverse-proxy deployments.
   * Override with `window.__PRAMAN_API_BASE_URL__` when needed.
   */
  apiBaseUrl: resolveApiBaseUrl(),
  /**
   * Appended to `POST …/objective-elements/{oeId}/evidence` for **file multipart** uploads only.
   * Default `''` (same path as JSON `postEvidenceJson`). If the server binds **JSON only** on `/evidence`
   * and exposes multipart on another route (e.g. `…/evidence/upload`), set `'/upload'` here.
   */
  evidenceMultipartPathSuffix: '',
  oauthAuthority: '',
  appName: 'AOVIAS Command',
  enableDebugLogging: true,
  /** When true, Journey uses `JourneyMockWorkspaceService` instead of split REST APIs. */
  useJourneyMockWorkspace: false,
  /**
   * OE wizard Evidence step: file picker, drag/drop, and Gateway multipart upload. Rollback: set false.
   * @see .planning/phases/54.2-evidence-upload-controlled-activation/
   */
  enableOeWizardEvidenceUpload: true,
  /**
   * Path segment after `apiBaseUrl` for Knowledge Base (code-keyed GETs per gateway spec).
   * @see ComplaincePlatform/docs/backend/gateway-knowledge-base-api-specification.md
   */
  knowledgeBasePath: 'knowledge-base',
  /** Journey split-read cache in sessionStorage (memory cache is always enabled). */
  journeyCacheSessionStorageEnabled: true,
  /** Enable chapter list virtual scroll in execution panel. */
  journeyVirtualScrollChaptersEnabled: true,
  /** Enable OE list virtual scroll inside expanded standard cards. */
  journeyVirtualScrollObjectiveElementsEnabled: true,
  /** Enable objective list virtual scroll in evidence binder (activates when length > 40). */
  binderVirtualScrollObjectivesEnabled: true,
  /**
   * Max client wait for Journey long reads (session, dashboard, chapters index, chapter standards, OE detail).
   * Must be >= Gateway / reverse-proxy limits — see `docs/backend/journey-screen-api-spec.md` §1.1.
   * Set to **0** to disable RxJS `timeout` for these requests (browser / infra limits still apply).
   */
  journeyLongReadTimeoutMs: 120_000,
  /** Dev-only login/dashboard performance timeline + HTTP timing report (`window.__AOVIAS_PERF_REPORT__`). */
  enablePerformanceTimeline: true,
  /** Program overview snapshot client cache TTL (ms). */
  dashboardSnapshotCacheTtlMs: 3 * 60 * 1000,
  /** Control Center overview cache TTL before background refresh on revisit (ms). */
  controlCenterCacheTtlMs: 60_000,
  /** Per-widget feature flags keyed by widget id. Omitted ids default to enabled. */
  dashboardWidgetFlags: {} as Record<string, boolean>,
};
