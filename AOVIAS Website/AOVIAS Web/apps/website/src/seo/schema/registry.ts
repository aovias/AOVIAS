/**
 * Schema.org type registry — supported structured data types for AOVIAS.
 * Pages should only emit types listed here from real on-page content.
 */
export const schemaTypeRegistry = {
  Organization: {
    id: 'Organization',
    description: 'AOVIAS organization entity (root graph)',
    builder: 'buildOrganizationSchema',
  },
  WebSite: {
    id: 'WebSite',
    description: 'Site entity with future-ready SearchAction',
    builder: 'buildWebsiteSchema',
  },
  WebPage: {
    id: 'WebPage',
    description: 'Generic page entity',
    builder: 'buildWebPageSchema',
  },
  AboutPage: {
    id: 'AboutPage',
    description: 'Company / about page',
    builder: 'buildPageSchemas(pageType)',
  },
  ContactPage: {
    id: 'ContactPage',
    description: 'Contact & engagement page',
    builder: 'buildPageSchemas(pageType)',
  },
  CollectionPage: {
    id: 'CollectionPage',
    description: 'Hub / catalog pages (products, technology, resources, solutions)',
    builder: 'buildPageSchemas(pageType)',
  },
  BreadcrumbList: {
    id: 'BreadcrumbList',
    description: 'Interior-page breadcrumb trail',
    builder: 'buildBreadcrumbListSchema',
  },
  FAQPage: {
    id: 'FAQPage',
    description: 'On-page FAQ blocks only',
    builder: 'buildFaqPageSchema',
  },
  SoftwareApplication: {
    id: 'SoftwareApplication',
    description: 'Product detail pages',
    builder: 'buildProductSoftwareSchema',
  },
  Product: {
    id: 'Product',
    description: 'Commercial product offers when applicable',
    builder: 'feature-owned',
  },
  Article: {
    id: 'Article',
    description: 'Insights and general knowledge items',
    builder: 'buildArticleJsonLd',
  },
  BlogPosting: {
    id: 'BlogPosting',
    description: 'Blog articles under /resources/blog',
    builder: 'buildArticleJsonLd',
  },
  TechArticle: {
    id: 'TechArticle',
    description: 'Documentation items',
    builder: 'buildArticleJsonLd',
  },
  ItemList: {
    id: 'ItemList',
    description: 'Catalog listings (products, industries)',
    builder: 'feature-owned',
  },
  SearchAction: {
    id: 'SearchAction',
    description: 'Future site search — declared on WebSite',
    builder: 'buildWebsiteSchema',
  },
} as const;

export type SchemaTypeId = keyof typeof schemaTypeRegistry;
