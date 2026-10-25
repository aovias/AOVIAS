/**
 * AOVIAS SEO public API — preferred import surface for discoverability work.
 * Implementations live in `@/lib/seo`; entity graph and registries live here.
 */

export {
  buildAbsoluteUrl,
  buildBreadcrumbListSchema,
  buildCanonicalUrl,
  buildDefaultSiteSchemas,
  buildFaqPageSchema,
  buildOpenGraph,
  buildOrganizationReference,
  buildOrganizationSchema,
  buildPageSchemas,
  buildRobotsConfig,
  buildSitemapEntries,
  buildTwitterCard,
  buildWebPageSchema,
  buildWebsiteReference,
  buildWebsiteSchema,
  createPageMetadata,
  createRootMetadata,
  createSitemapRoute,
  getAppEnvironment,
  getRawSiteUrl,
  getSiteUrl,
  normalizePath,
  organizationNodeId,
  shouldAllowIndexing,
  validateJsonLd,
  websiteNodeId,
  type BuildPageSchemasInput,
  type CreatePageMetadataInput,
  type FaqSchemaItem,
  type JsonLd,
  type OpenGraphInput,
  type SchemaValidationIssue,
  type SchemaValidationResult,
  type SitemapRouteConfig,
  type TwitterCardInput,
  type TwitterCardType,
  type WebPageSchemaInput,
} from '@/lib/seo';

export {
  entityFragmentId,
  entityRegistry,
  getEntity,
  listEntities,
  type EntityId,
  type EntityKind,
  type EntityRegistry,
  type EntityRelation,
  type KnowledgeEntity,
} from './entities';
export { ogImages, type OgImageKey } from './images';
export { schemaTypeRegistry, type SchemaTypeId } from './schema/registry';
export { runSchemaSelfCheck } from './schema/self-check';
