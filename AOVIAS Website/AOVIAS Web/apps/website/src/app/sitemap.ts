import type { MetadataRoute } from 'next';

import { industryCatalog } from '@/features/industries/domain';
import {
  knowledgeCategories,
  knowledgeCatalog,
} from '@/features/knowledge/domain';
import { buildSitemapEntries, createSitemapRoute } from '@/lib/seo';

/**
 * Production sitemap — static registry (seoConfig.sitemap.routes) plus
 * dynamic collectors for knowledge detail, categories, and planned industries.
 *
 * Future collectors (CMS, changelog, etc.) should append via createSitemapRoute
 * without changing the builder API.
 */
export default function sitemap(): MetadataRoute.Sitemap {
  const knowledgeRoutes = knowledgeCatalog.map((item) =>
    createSitemapRoute(item.href, {
      changeFrequency: 'monthly',
      priority: 0.75,
    }),
  );

  const categoryRoutes = knowledgeCategories.map((category) =>
    createSitemapRoute(`/resources/categories/${category.slug}`, {
      changeFrequency: 'monthly',
      priority: 0.65,
    }),
  );

  const plannedIndustryRoutes = industryCatalog
    .filter((industry) => industry.status === 'planned')
    .map((industry) =>
      createSitemapRoute(industry.href, {
        changeFrequency: 'monthly',
        priority: 0.55,
      }),
    );

  return buildSitemapEntries([
    ...knowledgeRoutes,
    ...categoryRoutes,
    ...plannedIndustryRoutes,
  ]);
}
