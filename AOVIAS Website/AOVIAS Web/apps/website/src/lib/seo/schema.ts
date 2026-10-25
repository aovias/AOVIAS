import { seoConfig } from '@/config/seo';
import type { BreadcrumbItem } from '@/lib/navigation/breadcrumb-types';

import { buildAbsoluteUrl, buildCanonicalUrl, getSiteUrl } from './canonical';

/** Minimal JSON-LD object (extensible). */
export type JsonLd = Record<string, unknown>;

export type WebPageSchemaInput = {
  title: string;
  description: string;
  path: string;
  /** schema.org @type — WebPage, AboutPage, ContactPage, CollectionPage, etc. */
  type?: string;
};

export type FaqSchemaItem = {
  question: string;
  answer: string;
};

/** Stable Organization node id — referenced site-wide; emitted once at root. */
export function organizationNodeId(): string {
  return `${getSiteUrl()}/#organization`;
}

/** Stable WebSite node id. */
export function websiteNodeId(): string {
  return `${getSiteUrl()}/#website`;
}

/** Organization @id reference — prefer over duplicating full Organization. */
export function buildOrganizationReference(): JsonLd {
  return {
    '@type': 'Organization',
    '@id': organizationNodeId(),
  };
}

/** WebSite @id reference. */
export function buildWebsiteReference(): JsonLd {
  return {
    '@type': 'WebSite',
    '@id': websiteNodeId(),
  };
}

/**
 * Organization schema — site-wide entity (root layout only).
 */
export function buildOrganizationSchema(): JsonLd {
  const org = seoConfig.organization;
  return {
    '@context': 'https://schema.org',
    '@type': 'Organization',
    '@id': organizationNodeId(),
    name: org.name,
    legalName: org.legalName,
    url: org.url,
    logo: buildAbsoluteUrl(org.logoPath),
    description: org.description,
    email: org.contactEmail,
    sameAs: [...org.sameAs],
  };
}

/**
 * WebSite schema — supports future SearchAction.
 */
export function buildWebsiteSchema(): JsonLd {
  const site = seoConfig.website;
  const origin = getSiteUrl();

  return {
    '@context': 'https://schema.org',
    '@type': 'WebSite',
    '@id': websiteNodeId(),
    name: site.name,
    description: site.description,
    url: `${origin}/`,
    publisher: buildOrganizationReference(),
    potentialAction: {
      '@type': 'SearchAction',
      target: {
        '@type': 'EntryPoint',
        urlTemplate: `${origin}${site.potentialAction.targetTemplate}`,
      },
      'query-input': site.potentialAction.queryInput,
    },
  };
}

/**
 * WebPage schema for a specific path.
 */
export function buildWebPageSchema(input: WebPageSchemaInput): JsonLd {
  return {
    '@context': 'https://schema.org',
    '@type': input.type ?? 'WebPage',
    name: input.title,
    description: input.description,
    url: buildCanonicalUrl(input.path),
    isPartOf: buildWebsiteReference(),
    inLanguage: seoConfig.language,
  };
}

/**
 * BreadcrumbList schema from breadcrumb items.
 */
export function buildBreadcrumbListSchema(items: readonly BreadcrumbItem[]): JsonLd {
  return {
    '@context': 'https://schema.org',
    '@type': 'BreadcrumbList',
    itemListElement: items.map((item, index) => {
      const element: Record<string, unknown> = {
        '@type': 'ListItem',
        position: index + 1,
        name: item.label,
      };
      if (item.href) {
        element['item'] = buildCanonicalUrl(item.href);
      }
      return element;
    }),
  };
}

/**
 * FAQPage schema — only from real on-page Q&A content.
 */
export function buildFaqPageSchema(
  items: readonly FaqSchemaItem[],
  options: { path?: string } = {},
): JsonLd {
  const schema: JsonLd = {
    '@context': 'https://schema.org',
    '@type': 'FAQPage',
    mainEntity: items.map((item) => ({
      '@type': 'Question',
      name: item.question,
      acceptedAnswer: {
        '@type': 'Answer',
        text: item.answer,
      },
    })),
  };

  if (options.path) {
    schema['url'] = buildCanonicalUrl(options.path);
  }

  return schema;
}

/**
 * Site-wide default graph: Organization + WebSite.
 */
export function buildDefaultSiteSchemas(): JsonLd[] {
  return [buildOrganizationSchema(), buildWebsiteSchema()];
}

export type BuildPageSchemasInput = {
  title: string;
  description: string;
  path: string;
  breadcrumbs?: readonly BreadcrumbItem[];
  /**
   * When true, attach publisher @id to the page (does not re-emit full Organization).
   * Full Organization lives in the root layout graph.
   */
  includeOrganization?: boolean;
  /** schema.org page type — AboutPage, ContactPage, CollectionPage, WebPage. */
  pageType?: string;
  /** Optional FAQ items rendered on the page. */
  faq?: readonly FaqSchemaItem[];
};

/**
 * Compose page-level schemas (WebPage + optional breadcrumbs + FAQ).
 */
export function buildPageSchemas(input: BuildPageSchemasInput): JsonLd[] {
  const schemas: JsonLd[] = [];

  const page = buildWebPageSchema({
    title: input.title,
    description: input.description,
    path: input.path,
    type: input.pageType ?? 'WebPage',
  });

  if (input.includeOrganization) {
    page['publisher'] = buildOrganizationReference();
  }

  schemas.push(page);

  if (input.breadcrumbs && input.breadcrumbs.length > 0) {
    schemas.push(buildBreadcrumbListSchema(input.breadcrumbs));
  }

  if (input.faq && input.faq.length > 0) {
    schemas.push(buildFaqPageSchema(input.faq, { path: input.path }));
  }

  return schemas;
}
