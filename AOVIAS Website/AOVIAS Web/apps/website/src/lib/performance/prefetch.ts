/**
 * Prefetch helpers for likely next navigations / secondary resources.
 * Prefer Next.js `<Link prefetch>` for routes; use these for explicit control.
 */

export type PrefetchDescriptor = {
  href: string;
  as?: 'document' | 'script' | 'style' | 'image' | 'fetch';
  crossOrigin?: 'anonymous' | 'use-credentials';
};

/**
 * Build `<link rel="prefetch" />` props for secondary resources.
 */
export function createPrefetchLink(descriptor: PrefetchDescriptor) {
  return {
    rel: 'prefetch' as const,
    href: descriptor.href,
    ...(descriptor.as ? { as: descriptor.as } : {}),
    ...(descriptor.crossOrigin ? { crossOrigin: descriptor.crossOrigin } : {}),
  };
}

/**
 * Prefetch a likely next document/route (low priority).
 */
export function prefetchRoute(href: string) {
  return createPrefetchLink({ href, as: 'document' });
}

/**
 * DNS / connection hints for approved third parties (use sparingly).
 */
export function createPreconnectLink(href: string, crossOrigin = true) {
  return {
    rel: 'preconnect' as const,
    href,
    ...(crossOrigin ? { crossOrigin: 'anonymous' as const } : {}),
  };
}

export function createDnsPrefetchLink(href: string) {
  return {
    rel: 'dns-prefetch' as const,
    href,
  };
}
