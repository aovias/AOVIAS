import type { ProductDefinition } from '@/features/products/domain';
import type { ProductDetailContent } from '@/features/products/detail/types';
import {
  productMedia,
  standardProductNav,
} from '@/features/products/detail/shared';

const visionFraming: Record<
  string,
  { problem: string; purpose: string; platformFit: string }
> = {
  sentinel: {
    problem:
      'Institutions need continuous assurance without drowning operators in undifferentiated alerts or opaque automation.',
    purpose:
      'AOVIAS Sentinel is planned as an assurance and oversight surface — vigilance with clear escalation and ownership.',
    platformFit:
      'Sentinel is intended to complement Command’s operations plane with assurance patterns that respect institutional duty.',
  },
  nexus: {
    problem:
      'Institutional stacks fragment when every product invents its own connectivity model and ownership boundaries.',
    purpose:
      'AOVIAS Nexus is planned as an interoperability foundation for coherent, governed connectivity across systems.',
    platformFit:
      'Nexus is intended to reinforce shared platform standards so Command, Studio, and future products remain coherent.',
  },
  insight: {
    problem:
      'Decision support fails institutions when analysis is opaque, unattributable, or detached from human accountability.',
    purpose:
      'AOVIAS Insight is planned as a measured intelligence surface that keeps human judgment accountable.',
    platformFit:
      'Insight is intended to apply responsible analysis on the AOVIAS platform — never as a black box that obscures ownership.',
  },
  flow: {
    problem:
      'Operational work breaks when processes cannot survive handoffs, staffing changes, or long institutional timelines.',
    purpose:
      'AOVIAS Flow is planned for durable institutional workflows and continuity across teams and time.',
    platformFit:
      'Flow is intended to deepen process continuity alongside Command — structured ownership without process theater.',
  },
};

/**
 * Future product detail — vision pages with distinct purpose per product.
 * Does not commit to schedules, packaging, or commercial availability.
 */
export function createFutureProductDetail(
  product: ProductDefinition,
): ProductDetailContent {
  const framing = visionFraming[product.slug] ?? {
    problem: product.description,
    purpose: product.summary,
    platformFit: `${product.name} is intended to extend the AOVIAS platform with shared standards for security, identity, and interoperability.`,
  };

  const showcases = product.featureSummary.map((feature, index) => ({
    id: `${product.slug}-capability-${index + 1}`,
    title: feature,
    description: `${feature} — a planned strength of ${product.name}, presented as direction rather than a shipped feature.`,
    media: productMedia(`${product.name} — ${feature}`),
    align: (index % 2 === 0 ? 'end' : 'start') as 'start' | 'end',
  }));

  return {
    slug: product.slug,
    status: product.status,
    name: product.name,
    tagline: product.summary,
    description: product.description,
    badge: 'Vision',
    ...(product.highlights ? { highlights: product.highlights } : {}),
    heroMedia: productMedia(`${product.name} — vision overview`),
    primaryCta: {
      label: 'Explore the portfolio',
      href: '/products',
    },
    secondaryCta: {
      label: 'Talk with AOVIAS',
      href: '/company/contact#form-contact',
    },
    seo: {
      title: `${product.name} — Platform Direction`,
      description: `${product.description} This page describes long-term platform direction — not availability or release timing.`,
      keywords: [
        product.name,
        'AOVIAS',
        'platform direction',
        'institutional technology',
      ],
    },
    navigation: standardProductNav,
    overview: {
      heading: `Why ${product.name} is on the roadmap`,
      description: `${framing.problem} ${framing.purpose}`,
      audience:
        'This vision page explains purpose and platform fit. It does not commit to schedules, packaging, or commercial availability.',
      media: productMedia(`${product.name} overview`),
      metrics: [
        { id: 'purpose', value: 'Purpose', label: 'Institutional need' },
        { id: 'fit', value: 'Platform', label: 'Coherent fit' },
        { id: 'horizon', value: 'Vision', label: 'Long-term horizon' },
      ],
    },
    capabilities: {
      heading: 'Intended capabilities',
      description:
        'Capability themes under consideration — direction for evaluation conversations, not a shipped feature list.',
      showcases,
      summaryItems: product.featureSummary.map((feature, index) => ({
        id: `${product.slug}-summary-${index + 1}`,
        title: feature,
        description: `Planned contribution of ${product.name} within the AOVIAS platform.`,
        category: 'Vision',
      })),
    },
    architecture: {
      title: 'Platform positioning',
      description: framing.platformFit,
      diagram: productMedia(`${product.name} architecture vision`),
      caption:
        'Vision positioning on the AOVIAS platform — direction, not a committed architecture sheet.',
    },
    integrations: {
      heading: 'Integration philosophy',
      description:
        'Future integrations will follow governed boundaries and institutional ownership — consistent with the AOVIAS platform.',
      items: [
        {
          id: `${product.slug}-identity`,
          name: 'Identity providers',
          description: 'Enterprise identity foundations',
          category: 'Security',
        },
        {
          id: `${product.slug}-platform`,
          name: 'AOVIAS platform',
          description: 'Shared platform services',
          category: 'Platform',
        },
        {
          id: `${product.slug}-systems`,
          name: 'Institutional systems',
          description: 'Governed connectivity patterns',
          category: 'Data',
        },
      ],
    },
    specifications: {
      heading: 'Technical direction',
      description:
        'Intended evaluation dimensions — not a committed specification sheet.',
      categories: [
        {
          id: 'platform',
          title: 'Platform',
          rows: [
            { id: 'role', label: 'Intended role', value: product.summary },
            { id: 'status', label: 'Status', value: 'Vision / roadmap' },
          ],
        },
        {
          id: 'deployment',
          title: 'Deployment',
          rows: [
            {
              id: 'model',
              label: 'Delivery model',
              value: 'To be defined with enterprise options',
            },
            {
              id: 'tenancy',
              label: 'Tenancy',
              value: 'Institution-scoped environments (intended)',
            },
          ],
        },
        {
          id: 'authentication',
          title: 'Authentication',
          rows: [
            {
              id: 'auth',
              label: 'Identity',
              value: 'Enterprise identity integration (intended)',
            },
            {
              id: 'access',
              label: 'Access control',
              value: 'Role-aware authorization (intended)',
            },
          ],
        },
        {
          id: 'apis',
          title: 'APIs',
          rows: [
            {
              id: 'integration',
              label: 'Integration style',
              value: 'Governed platform APIs (intended)',
            },
            {
              id: 'interop',
              label: 'Interoperability',
              value: 'Aligned with AOVIAS platform standards',
            },
          ],
        },
        {
          id: 'environments',
          title: 'Supported environments',
          rows: [
            {
              id: 'industries',
              label: 'Target contexts',
              value: product.industries.join(', '),
            },
            {
              id: 'availability',
              label: 'Availability',
              value: 'Not commercially available',
            },
          ],
        },
      ],
    },
    trust: {
      heading: 'Trust posture',
      description:
        'Vision products inherit AOVIAS trust principles: measured claims, clear ownership, and security as a fundamental — even before commercial availability.',
      signals: [
        {
          id: 'responsibility',
          label: 'Responsible direction',
          description: 'Purpose before promotion',
          variant: 'compliance',
        },
        {
          id: 'security',
          label: 'Security-minded',
          description: 'Controls as design constraints',
          variant: 'security',
        },
        {
          id: 'continuity',
          label: 'Long-term view',
          description: 'Roadmap without false urgency',
          variant: 'quality',
        },
      ],
      highlights: [
        'No availability or timeline claims on this page',
        'Platform coherence over product fragmentation',
        'Human accountability retained as intelligence expands',
      ],
      controls: [
        {
          id: 'claims',
          title: 'Measured claims',
          description: 'Vision language stays explicit about non-commitment.',
          category: 'Responsibility',
        },
        {
          id: 'boundaries',
          title: 'Platform boundaries',
          description: 'Intended to share identity and security foundations.',
          category: 'Security',
        },
        {
          id: 'stewardship',
          title: 'Stewardship',
          description: 'Designed for institutional duty, not novelty cycles.',
          category: 'Commitment',
        },
      ],
      frameworks: [
        {
          id: 'governance',
          name: 'Governance alignment',
          description: 'Intended to support institutional oversight practices.',
        },
        {
          id: 'data',
          name: 'Data boundaries',
          description: 'Institution-scoped environments as the default intent.',
        },
      ],
    },
    pricingEntry: {
      title: 'Availability',
      description:
        `${product.name} is not commercially available. Explore available products or talk with AOVIAS about platform direction.`,
      primaryCta: {
        label: 'View available products',
        href: '/products',
      },
      secondaryCta: {
        label: 'Talk with AOVIAS',
        href: '/company/contact#form-contact',
      },
    },
    resources: {
      heading: 'Related reading',
      description: 'Context for evaluating AOVIAS platform direction.',
      items: [
        {
          id: `${product.slug}-platform-docs`,
          kind: 'documentation',
          title: 'Platform overview',
          description: 'Orientation to AOVIAS products and operating models.',
          href: '/resources/docs/platform-overview',
          category: { id: 'docs', name: 'Documentation', tone: 'primary' },
        },
        {
          id: `${product.slug}-trust`,
          kind: 'article',
          title: 'Institutional trust',
          description: 'How clarity and accountability shape dependable systems.',
          href: '/resources/blog/institutional-trust',
          category: { id: 'guides', name: 'Guides', tone: 'info' },
        },
      ],
    },
    primaryCtaBand: {
      title: 'Continue exploring AOVIAS',
      description:
        'Review available products, or talk with AOVIAS about how platform direction may fit your institution.',
      primaryCta: {
        label: 'Explore products',
        href: '/products',
      },
      secondaryCta: {
        label: 'Talk with AOVIAS',
        href: '/company/contact#form-contact',
      },
    },
  };
}
