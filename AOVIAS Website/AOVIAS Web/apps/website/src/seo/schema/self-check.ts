import {
  buildBreadcrumbListSchema,
  buildDefaultSiteSchemas,
  buildFaqPageSchema,
  buildPageSchemas,
  validateJsonLd,
  type SchemaValidationResult,
} from '@/lib/seo';

/**
 * Internal schema self-check for R1.6 quality gates.
 * Call from tooling or inspect in development — does not hit the network.
 */
export function runSchemaSelfCheck(): SchemaValidationResult {
  const graph = [
    ...buildDefaultSiteSchemas(),
    ...buildPageSchemas({
      title: 'Self-check',
      description: 'Schema builder self-check page',
      path: '/company',
      pageType: 'AboutPage',
      includeOrganization: true,
      breadcrumbs: [
        { label: 'Home', href: '/' },
        { label: 'Company', href: '/company', current: true },
      ],
      faq: [
        {
          question: 'What is AOVIAS?',
          answer:
            'AOVIAS builds institutional software for organizations society depends on.',
        },
      ],
    }),
    buildFaqPageSchema([
      {
        question: 'Where is the sitemap?',
        answer: 'At /sitemap.xml in production.',
      },
    ]),
    buildBreadcrumbListSchema([
      { label: 'Home', href: '/' },
      { label: 'Technology', href: '/technology', current: true },
    ]),
  ];

  return validateJsonLd(graph);
}
