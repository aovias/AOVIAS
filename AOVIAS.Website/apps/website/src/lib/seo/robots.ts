import type { MetadataRoute } from 'next';

import { seoConfig } from '@/config/seo';

import { getSiteUrl } from './canonical';
import { shouldAllowIndexing } from './env';

/**
 * Build robots.txt rules — environment aware.
 * Non-production: disallow all. Production: allow with protected paths.
 */
export function buildRobotsConfig(): MetadataRoute.Robots {
  const origin = getSiteUrl();

  if (!shouldAllowIndexing()) {
    return {
      rules: {
        userAgent: '*',
        disallow: '/',
      },
      sitemap: `${origin}/sitemap.xml`,
      host: origin,
    };
  }

  const disallow = [
    ...seoConfig.robots.disallow,
    ...seoConfig.robots.previewDisallow,
    ...seoConfig.robots.adminDisallow,
  ];

  return {
    rules: [
      {
        userAgent: '*',
        allow: '/',
        disallow: [...disallow],
      },
    ],
    sitemap: `${origin}/sitemap.xml`,
    host: origin,
  };
}
