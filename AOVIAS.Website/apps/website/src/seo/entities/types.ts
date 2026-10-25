/**
 * Entity & Knowledge Graph types for consistent naming across
 * metadata, structured data, and internal linking.
 */

export type EntityKind =
  | 'Organization'
  | 'SoftwareApplication'
  | 'Product'
  | 'DefinedTerm'
  | 'Thing'
  | 'Service';

export type EntityId =
  | 'aovias'
  | 'aovias-platform'
  | 'aovias-command'
  | 'aovias-studio'
  | 'technology'
  | 'ai'
  | 'industries'
  | 'platform-services';

export type EntityRelation = {
  /** Predicate describing the relationship (e.g. makesOffer, isPartOf). */
  predicate: string;
  /** Target entity id. */
  target: EntityId;
};

export type KnowledgeEntity = {
  id: EntityId;
  kind: EntityKind;
  /** Canonical display name — use everywhere. */
  name: string;
  /** Short alternate names / aliases (no fabricated brands). */
  alternateNames?: readonly string[];
  description: string;
  /** Primary public URL path (site-relative). */
  path: string;
  /** Schema.org @type override when kind is too coarse. */
  schemaType?: string;
  /** Related entities for knowledge-graph consistency. */
  relations?: readonly EntityRelation[];
  /** Suggested OG image path when entity owns a page. */
  ogImage?: string;
};
