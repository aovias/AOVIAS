import type { ContentItem } from '@/components/content';
import { buildCanonicalUrl } from '@/lib/seo/canonical';
import {
  buildOrganizationReference,
  type JsonLd,
} from '@/lib/seo/schema';

export const knowledgeHubSeo = {
  title: 'Knowledge — Enterprise Learning Platform',
  description:
    'AOVIAS Knowledge — an enterprise knowledge platform for architecture, engineering, governance, AI, operations, documentation, and industry insight.',
  path: '/resources',
  keywords: [
    'AOVIAS knowledge',
    'AOVIAS documentation',
    'AOVIAS blog',
    'AOVIAS insights',
    'enterprise learning',
    'institutional technology',
  ],
} as const;

function resolveArticleType(item: ContentItem): string {
  if (item.kind === 'documentation') return 'TechArticle';
  if (item.kind === 'article') return 'BlogPosting';
  return 'Article';
}

export function buildArticleJsonLd(item: ContentItem): JsonLd {
  return {
    '@context': 'https://schema.org',
    '@type': resolveArticleType(item),
    headline: item.title,
    description: item.description,
    url: buildCanonicalUrl(item.href),
    datePublished: item.publishedAt,
    dateModified: item.updatedAt ?? item.publishedAt,
    author: item.authors?.map((author) => ({
      '@type': 'Person',
      name: author.name,
    })),
    publisher: buildOrganizationReference(),
    mainEntityOfPage: buildCanonicalUrl(item.href),
  };
}
