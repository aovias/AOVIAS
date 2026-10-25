import type { EntityId, KnowledgeEntity } from './types';

/**
 * Canonical AOVIAS entity registry.
 * Single source of truth for naming, paths, and semantic relationships.
 * Do not invent certifications, customers, or unreleased product claims.
 */
export const entityRegistry = {
  aovias: {
    id: 'aovias',
    kind: 'Organization',
    name: 'AOVIAS',
    alternateNames: ['AOVIAS Platform'],
    description:
      'AOVIAS strengthens institutions through intelligent, responsible technology — built with engineering discipline and long-term commitment.',
    path: '/company',
    schemaType: 'Organization',
    ogImage: '/og/company.png',
    relations: [
      { predicate: 'makesOffer', target: 'aovias-command' },
      { predicate: 'makesOffer', target: 'aovias-studio' },
      { predicate: 'owns', target: 'aovias-platform' },
    ],
  },
  'aovias-platform': {
    id: 'aovias-platform',
    kind: 'Thing',
    name: 'AOVIAS Platform',
    description:
      'The shared institutional platform direction connecting products, services, and engineering practice.',
    path: '/technology/platform',
    schemaType: 'WebPage',
    ogImage: '/og/technology.png',
    relations: [
      { predicate: 'isPartOf', target: 'aovias' },
      { predicate: 'includes', target: 'platform-services' },
      { predicate: 'relatedTo', target: 'technology' },
    ],
  },
  'aovias-command': {
    id: 'aovias-command',
    kind: 'SoftwareApplication',
    name: 'AOVIAS Command',
    alternateNames: ['Command'],
    description:
      'Institutional operations product for visibility, control, and accountable operational judgment.',
    path: '/products/command',
    schemaType: 'SoftwareApplication',
    ogImage: '/og/command.png',
    relations: [
      { predicate: 'provider', target: 'aovias' },
      { predicate: 'isRelatedTo', target: 'aovias-platform' },
    ],
  },
  'aovias-studio': {
    id: 'aovias-studio',
    kind: 'SoftwareApplication',
    name: 'AOVIAS Studio',
    alternateNames: ['Studio'],
    description:
      'Delivery workspace for designing, refining, and shipping institutional work with discipline.',
    path: '/products/studio',
    schemaType: 'SoftwareApplication',
    ogImage: '/og/studio.png',
    relations: [
      { predicate: 'provider', target: 'aovias' },
      { predicate: 'isRelatedTo', target: 'aovias-platform' },
    ],
  },
  technology: {
    id: 'technology',
    kind: 'DefinedTerm',
    name: 'Technology',
    description:
      'How AOVIAS designs and operates institutional technology — platform, AI, engineering, trust, and ecosystem.',
    path: '/technology',
    schemaType: 'CollectionPage',
    ogImage: '/og/technology.png',
    relations: [
      { predicate: 'isPartOf', target: 'aovias' },
      { predicate: 'includes', target: 'ai' },
      { predicate: 'includes', target: 'aovias-platform' },
    ],
  },
  ai: {
    id: 'ai',
    kind: 'DefinedTerm',
    name: 'Artificial Intelligence',
    alternateNames: ['AI', 'AOVIAS AI'],
    description:
      'Responsible AI practice at AOVIAS — amplifying human judgment with clear accountability boundaries.',
    path: '/technology/ai',
    schemaType: 'CollectionPage',
    ogImage: '/og/technology.png',
    relations: [
      { predicate: 'isPartOf', target: 'technology' },
      { predicate: 'relatedTo', target: 'aovias-platform' },
    ],
  },
  industries: {
    id: 'industries',
    kind: 'DefinedTerm',
    name: 'Industries',
    alternateNames: ['Solutions', 'Industry Experience'],
    description:
      'Industry experience framing for healthcare, government, education, and enterprise institutions.',
    path: '/solutions',
    schemaType: 'CollectionPage',
    ogImage: '/og/solutions.png',
    relations: [{ predicate: 'isPartOf', target: 'aovias' }],
  },
  'platform-services': {
    id: 'platform-services',
    kind: 'Service',
    name: 'Platform Services',
    description:
      'Shared platform services that support products and institutional deployments on the AOVIAS platform.',
    path: '/technology/platform/services',
    schemaType: 'Service',
    ogImage: '/og/technology.png',
    relations: [
      { predicate: 'isPartOf', target: 'aovias-platform' },
      { predicate: 'provider', target: 'aovias' },
    ],
  },
} as const satisfies Record<EntityId, KnowledgeEntity>;

export type EntityRegistry = typeof entityRegistry;

export function getEntity(id: EntityId): KnowledgeEntity {
  return entityRegistry[id];
}

export function listEntities(): readonly KnowledgeEntity[] {
  return Object.values(entityRegistry);
}

/** Stable JSON-LD @id for an entity (fragment on site origin). */
export function entityFragmentId(id: EntityId): string {
  return `#entity-${id}`;
}
