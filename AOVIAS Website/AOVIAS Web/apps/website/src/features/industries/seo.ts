import {
  getAvailableIndustries,
  industryCatalog,
} from '@/features/industries/domain';
import { buildCanonicalUrl } from '@/lib/seo/canonical';
import { buildWebsiteReference, type JsonLd } from '@/lib/seo/schema';

export const solutionsSeo = {
  title: 'Solutions — Healthcare, Government, Manufacturing & Financial Services',
  description:
    'How AOVIAS strengthens institutions across healthcare, government, manufacturing, and financial services — shared platform architecture with sector-specific operational understanding.',
  path: '/solutions',
  keywords: [
    'AOVIAS solutions',
    'industry solutions',
    'healthcare technology',
    'government technology',
    'manufacturing operations',
    'financial services platform',
  ],
} as const;

export function buildIndustryCatalogSchema(): JsonLd {
  const items = getAvailableIndustries().map((industry, index) => ({
    '@type': 'ListItem',
    position: index + 1,
    name: industry.name,
    description: industry.summary,
    url: buildCanonicalUrl(industry.href),
  }));

  return {
    '@context': 'https://schema.org',
    '@type': 'ItemList',
    name: 'AOVIAS industry solutions',
    numberOfItems: items.length,
    itemListElement: items,
  };
}

type IndustrySchemaInput = {
  slug: string;
  name: string;
  seo: { title: string; description: string };
  overviewDescription: string;
};

/**
 * WebPage-oriented industry schema with audience framing.
 */
export function buildIndustryPageSchema(content: IndustrySchemaInput): JsonLd {
  return {
    '@context': 'https://schema.org',
    '@type': 'WebPage',
    name: content.seo.title,
    description: content.seo.description,
    url: buildCanonicalUrl(`/solutions/${content.slug}`),
    about: {
      '@type': 'Thing',
      name: content.name,
      description: content.overviewDescription,
    },
    audience: {
      '@type': 'Audience',
      audienceType: content.name,
    },
    isPartOf: buildWebsiteReference(),
  };
}

export function getIndustrySitemapPaths(): string[] {
  return industryCatalog
    .filter((industry) => industry.status === 'available')
    .map((industry) => industry.href);
}
