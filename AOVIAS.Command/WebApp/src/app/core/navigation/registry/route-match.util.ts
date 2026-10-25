/** Normalize URL to pathname without query/hash/trailing slash. */
export function normalizeNavPath(url: string): string {
  return url.split(/[?#]/)[0].replace(/\/+$/, '') || '/';
}

export function isJourneyShellNavPath(url: string): boolean {
  const path = normalizeNavPath(url);
  if (path === '/app/journey') return true;
  if (path.startsWith('/app/journey/execution')) return true;
  if (path === '/app/journey/maturity') return true;
  if (path === '/app/journey/operational') return true;
  return false;
}

export function isDocumentationShellNavPath(url: string): boolean {
  const path = normalizeNavPath(url);
  return path === '/app/documentation' || path.startsWith('/app/documentation/');
}

export function isPatientSafetyShellNavPath(url: string): boolean {
  const path = normalizeNavPath(url);
  return path === '/app/patient-safety' || path.startsWith('/app/patient-safety/');
}

export function isQualityIntelligenceShellNavPath(url: string): boolean {
  const path = normalizeNavPath(url);
  return path === '/app/quality-intelligence' || path.startsWith('/app/quality-intelligence/');
}

export function isComplianceGraphShellNavPath(url: string): boolean {
  const path = normalizeNavPath(url);
  return path === '/app/compliance-graph' || path.startsWith('/app/compliance-graph/');
}

export function isRisksShellNavPath(url: string): boolean {
  const path = normalizeNavPath(url);
  return path === '/app/risks' || path.startsWith('/app/risks/');
}

/**
 * Match a registered route against the current URL.
 * Longer routes win; exactActive requires equality; subsetActive uses prefix.
 */
export function matchRoute(
  url: string,
  route: string,
  opts?: { exactActive?: boolean; subsetActive?: boolean },
): boolean {
  const path = normalizeNavPath(url);
  const target = normalizeNavPath(route);
  if (opts?.exactActive) return path === target;
  if (opts?.subsetActive) return path === target || path.startsWith(`${target}/`);
  return path === target || path.startsWith(`${target}/`);
}
