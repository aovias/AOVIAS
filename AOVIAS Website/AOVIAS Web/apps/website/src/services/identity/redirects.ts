/**
 * Secure redirect handling — allowlist only; never trust raw return URLs.
 */

const DEFAULT_POST_AUTH = '/';

const ALLOWED_PREFIXES = [
  '/',
  '/products',
  '/resources',
  '/company',
  '/solutions',
  '/pricing',
  '/checkout',
] as const;

/**
 * Resolve a post-auth redirect. Rejects protocol-relative and external URLs.
 */
export function resolveSafeRedirect(
  candidate: string | null | undefined,
  fallback: string = DEFAULT_POST_AUTH,
): string {
  if (!candidate) return fallback;

  const value = candidate.trim();
  if (!value.startsWith('/') || value.startsWith('//')) {
    return fallback;
  }
  if (value.includes('://') || value.includes('\\')) {
    return fallback;
  }

  const pathOnly = value.split('?')[0] ?? value;
  const allowed = ALLOWED_PREFIXES.some(
    (prefix) => pathOnly === prefix || pathOnly.startsWith(`${prefix}/`),
  );

  if (!allowed && pathOnly !== '/') {
    return fallback;
  }

  return value;
}
