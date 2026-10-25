import type { MetadataRoute } from 'next';

import { seoConfig } from '@/config/seo';

import { buildCanonicalUrl } from './canonical';
import { shouldAllowIndexing } from './env';

export type SitemapRouteConfig = {
  path: string;
  changeFrequency?: MetadataRoute.Sitemap[number]['changeFrequency'];
  priority?: number;
  lastModified?: Date | string;
};

/**
 * Build sitemap entries from the centralized public route registry.
 * Returns an empty sitemap outside production (previews/dev not indexed).
 */
export function buildSitemapEntries(
  extraRoutes: readonly SitemapRouteConfig[] = [],
): MetadataRoute.Sitemap {
  if (!shouldAllowIndexing()) {
    return [];
  }

  const routes = [...seoConfig.sitemap.routes, ...extraRoutes];
  const now = new Date();

  return routes.map((route) => {
    const lastModified =
      'lastModified' in route && route.lastModified
        ? new Date(route.lastModified)
        : now;

    return {
      url: buildCanonicalUrl(route.path),
      lastModified,
      changeFrequency:
        route.changeFrequency ?? seoConfig.sitemap.defaultChangeFrequency,
      priority: route.priority ?? seoConfig.sitemap.defaultPriority,
    };
  });
}

/** Register helper for future dynamic route collectors. */
export function createSitemapRoute(
  path: string,
  options: Omit<SitemapRouteConfig, 'path'> = {},
): SitemapRouteConfig {
  return { path, ...options };
}
