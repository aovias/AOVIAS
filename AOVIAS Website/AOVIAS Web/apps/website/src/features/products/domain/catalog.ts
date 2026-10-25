import type {
  ProductCategoryMeta,
  ProductDefinition,
  ProductGroupDefinition,
} from '@/features/products/domain/types';

/**
 * AOVIAS product catalog — single source of truth for portfolio presentation.
 * Future products: add a definition here; overview and nav derive from this list.
 */

export const productCategories: readonly ProductCategoryMeta[] = [
  {
    id: 'platform',
    label: 'Platform',
    description: 'Core systems for institutional delivery and operations.',
  },
  {
    id: 'intelligence',
    label: 'Intelligence',
    description: 'Responsible insight and decision-support capabilities.',
  },
  {
    id: 'operations',
    label: 'Operations',
    description: 'Workflow, continuity, and operational resilience.',
  },
] as const;

export const productCatalog: readonly ProductDefinition[] = [
  {
    id: 'command',
    name: 'AOVIAS Command',
    slug: 'command',
    category: 'platform',
    description:
      'Operational clarity for complex environments — structured workflows, visibility, and control where reliability matters.',
    summary:
      'Command and control for teams that need dependable operational visibility.',
    status: 'available',
    featureSummary: [
      'Operational visibility across critical workflows',
      'Structured control with clear ownership',
      'Designed for institutional reliability',
    ],
    industries: ['healthcare', 'government', 'enterprise'],
    pricingEntry: {
      label: 'Pricing',
      href: '/pricing',
    },
    documentationEntry: {
      label: 'Documentation',
      href: '/resources/docs',
    },
    demoAvailable: true,
    demoCta: {
      label: 'Request a demo',
      href: '/company/contact#form-demo',
    },
    highlights: [
      { id: 'visibility', label: 'Operational visibility' },
      { id: 'control', label: 'Structured control' },
      { id: 'reliability', label: 'Institutional reliability' },
    ],
    href: '/products/command',
    order: 1,
  },
  {
    id: 'studio',
    name: 'AOVIAS Studio',
    slug: 'studio',
    category: 'platform',
    description:
      'A disciplined space to design, refine, and deliver institutional solutions with consistency across teams.',
    summary:
      'Delivery workspace for teams building coherent institutional solutions.',
    status: 'available',
    featureSummary: [
      'Consistent solution design and delivery',
      'Shared standards across teams',
      'Built for long-lived institutional work',
    ],
    industries: ['healthcare', 'education', 'enterprise'],
    pricingEntry: {
      label: 'Pricing',
      href: '/pricing',
    },
    documentationEntry: {
      label: 'Documentation',
      href: '/resources/docs',
    },
    demoAvailable: true,
    demoCta: {
      label: 'Request a demo',
      href: '/company/contact#form-demo',
    },
    highlights: [
      { id: 'delivery', label: 'Disciplined delivery' },
      { id: 'consistency', label: 'Team consistency' },
      { id: 'stewardship', label: 'Long-term stewardship' },
    ],
    href: '/products/studio',
    order: 2,
  },
  {
    id: 'platform',
    name: 'AOVIAS Platform',
    slug: 'platform',
    category: 'platform',
    description:
      'Shared foundation services — identity, security, deployment, AI, and governance — that make Command and Studio stronger together.',
    summary:
      'The commercial foundation that compounds product value across the portfolio.',
    status: 'available',
    featureSummary: [
      'Shared identity and policy services',
      'Governed deployment and observability',
      'Security and AI governance as product fundamentals',
    ],
    industries: ['healthcare', 'government', 'enterprise'],
    pricingEntry: {
      label: 'Pricing',
      href: '/pricing',
    },
    documentationEntry: {
      label: 'Documentation',
      href: '/resources/docs',
    },
    demoAvailable: true,
    demoCta: {
      label: 'Request a demo',
      href: '/company/contact#form-demo',
    },
    highlights: [
      { id: 'identity', label: 'Shared identity' },
      { id: 'security', label: 'Security foundation' },
      { id: 'coherence', label: 'Portfolio coherence' },
    ],
    href: '/products/platform',
    order: 3,
  },
  {
    id: 'sentinel',
    name: 'AOVIAS Sentinel',
    slug: 'sentinel',
    category: 'operations',
    description:
      'A future capability focused on vigilance, assurance, and continuous institutional oversight.',
    summary: 'Assurance and oversight for environments that cannot afford surprise.',
    status: 'planned',
    featureSummary: [
      'Continuous assurance patterns',
      'Clear escalation and ownership',
      'Designed for regulated contexts',
    ],
    industries: ['healthcare', 'government', 'enterprise'],
    demoAvailable: false,
    href: '/products/sentinel',
    order: 10,
    roadmap: true,
  },
  {
    id: 'nexus',
    name: 'AOVIAS Nexus',
    slug: 'nexus',
    category: 'platform',
    description:
      'A future integration layer for coherent connectivity across institutional systems.',
    summary: 'Interoperability foundation for a connected AOVIAS ecosystem.',
    status: 'planned',
    featureSummary: [
      'Shared integration standards',
      'Governed connectivity',
      'Ecosystem coherence',
    ],
    industries: ['government', 'enterprise', 'education'],
    demoAvailable: false,
    href: '/products/nexus',
    order: 11,
    roadmap: true,
  },
  {
    id: 'insight',
    name: 'AOVIAS Insight',
    slug: 'insight',
    category: 'intelligence',
    description:
      'A future intelligence surface for responsible analysis and institutional decision support.',
    summary: 'Measured insight that keeps human judgment accountable.',
    status: 'planned',
    featureSummary: [
      'Human-centered intelligence',
      'Transparent analytical pathways',
      'Decision support without opacity',
    ],
    industries: ['healthcare', 'education', 'enterprise'],
    demoAvailable: false,
    href: '/products/insight',
    order: 12,
    roadmap: true,
  },
  {
    id: 'flow',
    name: 'AOVIAS Flow',
    slug: 'flow',
    category: 'operations',
    description:
      'A future operations product for durable institutional workflows and continuity.',
    summary: 'Workflow continuity for teams that carry lasting responsibility.',
    status: 'planned',
    featureSummary: [
      'Durable process design',
      'Operational continuity',
      'Clear handoffs and ownership',
    ],
    industries: ['healthcare', 'government', 'enterprise'],
    demoAvailable: false,
    href: '/products/flow',
    order: 13,
    roadmap: true,
  },
] as const;

export const productGroups: readonly ProductGroupDefinition[] = [
  {
    id: 'core-platform',
    label: 'Core platform',
    description: 'Products available today for institutional delivery and operations.',
    category: 'platform',
    productIds: ['command', 'studio', 'platform'],
  },
  {
    id: 'roadmap',
    label: 'Platform roadmap',
    description: 'Long-term capabilities that extend the AOVIAS ecosystem.',
    category: 'platform',
    productIds: ['sentinel', 'nexus', 'insight', 'flow'],
  },
] as const;

export function getProductBySlug(slug: string): ProductDefinition | undefined {
  return productCatalog.find((product) => product.slug === slug);
}

export function getProductById(id: string): ProductDefinition | undefined {
  return productCatalog.find((product) => product.id === id);
}

export function getAvailableProducts(): ProductDefinition[] {
  return productCatalog
    .filter((product) => product.status === 'available' || product.status === 'preview')
    .sort((a, b) => a.order - b.order);
}

export function getRoadmapProducts(): ProductDefinition[] {
  return productCatalog
    .filter((product) => product.roadmap || product.status === 'planned')
    .sort((a, b) => a.order - b.order);
}

export function getProductsByCategory(category: ProductDefinition['category']) {
  return productCatalog
    .filter((product) => product.category === category)
    .sort((a, b) => a.order - b.order);
}

export function getProductsForGroup(groupId: string): ProductDefinition[] {
  const group = productGroups.find((item) => item.id === groupId);
  if (!group) return [];
  return group.productIds
    .map((id) => getProductById(id))
    .filter((product): product is ProductDefinition => Boolean(product));
}
