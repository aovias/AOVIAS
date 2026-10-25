/**
 * Pathname matching helpers for primary navigation active states.
 */

export function normalizePath(pathname: string): string {
  if (!pathname) return '/';
  if (pathname === '/') return '/';
  return pathname.endsWith('/') ? pathname.slice(0, -1) : pathname;
}

/** Exact match, or nested path under href (excluding bare `/`). */
export function isActiveHref(pathname: string, href: string): boolean {
  const path = normalizePath(pathname);
  const target = normalizePath(href);

  if (target === '/') return path === '/';
  return path === target || path.startsWith(`${target}/`);
}

/** Prefer the longest matching href among candidates. */
export function findBestActiveHref(
  pathname: string,
  hrefs: readonly string[],
): string | null {
  const matches = hrefs
    .filter((href) => isActiveHref(pathname, href))
    .sort((a, b) => normalizePath(b).length - normalizePath(a).length);

  return matches[0] ?? null;
}
