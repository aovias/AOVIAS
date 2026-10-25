import { siteConfig } from '@/config/site';

import { getRawSiteUrl } from './env';

/**
 * Environment-aware site origin (no trailing slash).
 */
export function getSiteUrl(): string {
  return getRawSiteUrl() ?? siteConfig.url;
}

/**
 * Normalize a path to a leading-slash, no-trailing-slash form (except `/`).
 */
export function normalizePath(path: string): string {
  if (!path || path === '/') return '/';
  const withSlash = path.startsWith('/') ? path : `/${path}`;
  const withoutQuery = withSlash.split('?')[0]?.split('#')[0] ?? '/';
  if (withoutQuery.length > 1 && withoutQuery.endsWith('/')) {
    return withoutQuery.slice(0, -1);
  }
  return withoutQuery;
}

/**
 * Build an absolute canonical URL for a site path.
 */
export function buildCanonicalUrl(path = '/'): string {
  const origin = getSiteUrl();
  const normalized = normalizePath(path);
  if (normalized === '/') return `${origin}/`;
  return `${origin}${normalized}`;
}

/**
 * Resolve an asset path or absolute URL to an absolute URL on the site origin.
 */
export function buildAbsoluteUrl(pathOrUrl: string): string {
  if (/^https?:\/\//i.test(pathOrUrl)) return pathOrUrl;
  return buildCanonicalUrl(pathOrUrl);
}
