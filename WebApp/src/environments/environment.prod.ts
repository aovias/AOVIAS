const API_PATH = '/api/v1';
const DEV_ANGULAR_PORT = '4200';
const DEV_GATEWAY_PORT = '5000';

/** @see environment.ts — identical resolution strategy for prod build. */
function resolveGatewayPort(): string {
  if (typeof window === 'undefined') return DEV_GATEWAY_PORT;
  const currentPort = window.location.port;
  return currentPort || DEV_GATEWAY_PORT;
}

/** @see environment.ts */
function isNgDevServer(): boolean {
  return typeof window !== 'undefined' && window.location?.port === DEV_ANGULAR_PORT;
}

/** @see environment.ts — identical resolution strategy for prod build. */
function resolveApiBaseUrl(): string {
  const override = (globalThis as { __PRAMAN_API_BASE_URL__?: string }).__PRAMAN_API_BASE_URL__;
  if (override) return override;

  if (isNgDevServer()) {
    return API_PATH;
  }

  if (typeof window === 'undefined' || !window.location?.hostname) {
    return `http://localhost:${DEV_GATEWAY_PORT}${API_PATH}`;
  }

  const protocol = window.location.protocol;
  const host = window.location.hostname || 'localhost';
  const port = resolveGatewayPort();
  const portPart = port ? `:${port}` : '';
  return `${protocol}//${host}${portPart}${API_PATH}`;
}

export const environment = {
  production: true,
  apiBaseUrl: resolveApiBaseUrl(),
  /** @see environment.ts — multipart file POST path suffix after `…/evidence` */
  evidenceMultipartPathSuffix: '',
  oauthAuthority: '',
  appName: 'AOVIAS Command',
  enableDebugLogging: false,
  useJourneyMockWorkspace: false,
  /** Rollback: set false to disable OE wizard evidence picker + upload API. @see environment.ts */
  enableOeWizardEvidenceUpload: false,
  /** @see environment.ts */
  knowledgeBasePath: 'knowledge-base',
  journeyCacheSessionStorageEnabled: true,
  journeyVirtualScrollChaptersEnabled: true,
  journeyVirtualScrollObjectiveElementsEnabled: true,
  binderVirtualScrollObjectivesEnabled: true,
  /** @see environment.ts — align with production Gateway / proxy timeouts. */
  journeyLongReadTimeoutMs: 120_000,
  enablePerformanceTimeline: false,
  dashboardSnapshotCacheTtlMs: 3 * 60 * 1000,
  controlCenterCacheTtlMs: 60_000,
  dashboardWidgetFlags: {} as Record<string, boolean>,
};
