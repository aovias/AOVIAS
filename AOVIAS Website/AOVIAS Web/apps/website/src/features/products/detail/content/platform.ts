import { getProductBySlug } from '@/features/products/domain';
import type { ProductDetailContent } from '@/features/products/detail/types';
import {
  productMedia,
  standardProductNav,
} from '@/features/products/detail/shared';

const product = getProductBySlug('platform');

/**
 * AOVIAS Platform — shared foundation product detail (commercial).
 */
export const platformDetailContent = {
  slug: 'platform',
  status: 'available',
  name: product?.name ?? 'AOVIAS Platform',
  tagline:
    'The shared foundation that makes AOVIAS products stronger together.',
  description:
    product?.description ??
    'Identity, security, deployment, AI, and governance as business capability — not infrastructure theater.',
  badge: 'Available',
  atmosphere: 'blueprint',
  ...(product?.highlights ? { highlights: product.highlights } : {}),
  heroMedia: productMedia('AOVIAS Platform — services console'),
  secondaryHeroMedia: productMedia('Platform foundation workflow'),
  primaryCta: {
    label: 'Request a demo',
    href: '/company/contact#form-demo',
  },
  secondaryCta: {
    label: 'Explore products',
    href: '/products',
  },
  seo: {
    title: 'AOVIAS Platform — Shared Enterprise Foundation',
    description:
      'AOVIAS Platform provides identity, security, deployment, AI, and governance services that compound Command and Studio — a mature commercial foundation.',
    keywords: [
      'AOVIAS Platform',
      'enterprise platform',
      'shared foundation',
      'institutional software platform',
    ],
  },
  navigation: standardProductNav,
  overview: {
    heading: 'Why products are stronger on Platform',
    description:
      'Disconnected tools create fragmented identity, uneven security, and brittle deployments. Platform exists so Command and Studio share durable foundations — and so institutions adopt capability without redesigning their stack.',
    audience:
      'Platform is for enterprise architecture, security, and technology leaders who need a coherent foundation — business capability expressed as shared services, not a second operations console.',
    media: productMedia('Platform overview console'),
    metrics: [
      { id: 'identity', value: 'Identity', label: 'Shared access posture' },
      { id: 'security', value: 'Security', label: 'Controls as fundamentals' },
      { id: 'coherence', value: 'Coherence', label: 'Portfolio strength' },
    ],
  },
  capabilities: {
    heading: 'Foundation capabilities',
    description:
      'Platform concentrates on the services every commercial product inherits — without collapsing Command and Studio into one undifferentiated application.',
    showcases: [
      {
        id: 'identity',
        title: 'Identity that travels with the work',
        description:
          'Enterprise identity and role-aware access across Command and Studio — so ownership and authorization stay coherent.',
        media: productMedia('Platform identity capability'),
        align: 'end',
      },
      {
        id: 'ai',
        title: 'AI with accountability',
        description:
          'Assisted workflows under governance. Human judgment remains attributable; models do not become opaque operators.',
        media: productMedia('Platform AI capability workflow'),
        align: 'start',
      },
      {
        id: 'deployment',
        title: 'Deployment as institutional discipline',
        description:
          'Governed environments and release pathways that respect institutional constraints — not ad-hoc shipping.',
        media: productMedia('Platform deployment capability'),
        align: 'end',
      },
      {
        id: 'security',
        title: 'Security as product foundation',
        description:
          'Boundaries, auditability, and assurance patterns designed into the portfolio — not bolted on after demos.',
        media: productMedia('Platform security capability'),
        align: 'start',
      },
      {
        id: 'integration',
        title: 'Integration without sprawl',
        description:
          'Governed connectivity so institutional systems participate without creating a second source of truth.',
        media: productMedia('Platform integration capability'),
        align: 'end',
      },
    ],
    summaryItems: [
      {
        id: 'services',
        title: 'Shared services',
        description: 'Identity, policy, and observability as common layers.',
        category: 'Foundation',
      },
      {
        id: 'governance',
        title: 'Governance',
        description: 'Policies and evidence-friendly operational records.',
        category: 'Trust',
      },
      {
        id: 'principles',
        title: 'Platform principles',
        description: 'Clear product boundaries. Shared strength. No monolith.',
        category: 'Architecture',
      },
    ],
  },
  architecture: {
    title: 'One architecture illustration',
    description:
      'Platform sits beneath Command and Studio as the shared foundation — identity, policies, services, deployments, and observability in a single coherent picture.',
    diagram: productMedia('Platform architecture diagram'),
    caption:
      'Identity → Policies → Services → Deployments → Observability — the Platform spine that Command and Studio inherit.',
  },
  integrations: {
    heading: 'Integration surface',
    description:
      'Connect institutional systems through governed platform pathways — purpose-bound, attributable, and reviewable.',
    items: [
      {
        id: 'identity',
        name: 'Identity providers',
        description: 'Enterprise SSO and role foundations',
        category: 'Security',
      },
      {
        id: 'secrets',
        name: 'Secrets and keys',
        description: 'Institution-scoped credential handling',
        category: 'Security',
      },
      {
        id: 'observability',
        name: 'Observability pipelines',
        description: 'Signals that support operational judgment',
        category: 'Platform',
      },
      {
        id: 'apis',
        name: 'Platform APIs',
        description: 'Governed interfaces for Command and Studio',
        category: 'Integration',
      },
    ],
  },
  specifications: {
    heading: 'Evaluation dimensions',
    description:
      'Categories institutions typically assess when evaluating a commercial platform foundation.',
    categories: [
      {
        id: 'role',
        title: 'Platform role',
        rows: [
          {
            id: 'purpose',
            label: 'Product role',
            value: 'Shared commercial foundation',
          },
          {
            id: 'boundary',
            label: 'Product boundary',
            value: 'Services beneath Command and Studio',
          },
        ],
      },
      {
        id: 'deployment',
        title: 'Deployment',
        rows: [
          {
            id: 'model',
            label: 'Delivery model',
            value: 'Enterprise multi-region options',
          },
          {
            id: 'tenancy',
            label: 'Tenancy',
            value: 'Institution-scoped environments',
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
            value: 'Enterprise identity integration',
          },
          {
            id: 'access',
            label: 'Access control',
            value: 'Role-aware authorization',
          },
        ],
      },
      {
        id: 'governance',
        title: 'Governance',
        rows: [
          {
            id: 'policy',
            label: 'Policy',
            value: 'Shared policy engine across products',
          },
          {
            id: 'ai',
            label: 'AI governance',
            value: 'Assisted workflows with accountability',
          },
        ],
      },
    ],
  },
  trust: {
    heading: 'Trust and security',
    description:
      'Platform security posture is the baseline Command and Studio inherit — measured, attributable, enterprise-ready.',
    signals: [
      {
        id: 'security',
        label: 'Security-minded',
        description: 'Controls as product fundamentals',
        variant: 'security',
      },
      {
        id: 'governance',
        label: 'Governed',
        description: 'Policies and evidence pathways',
        variant: 'compliance',
      },
      {
        id: 'enterprise',
        label: 'Enterprise ready',
        description: 'Institutional constraints respected',
        variant: 'quality',
      },
    ],
    highlights: [
      'Shared identity and policy across the portfolio',
      'Auditability designed into foundation services',
      'Clear product boundaries — no forced monolith',
    ],
    controls: [
      {
        id: 'identity',
        title: 'Identity foundation',
        description: 'Enterprise identity as a shared platform service.',
        category: 'Security',
      },
      {
        id: 'policy',
        title: 'Policy engine',
        description: 'Consistent authorization patterns across products.',
        category: 'Governance',
      },
      {
        id: 'observability',
        title: 'Observability',
        description: 'Signals that support institutional judgment.',
        category: 'Reliability',
      },
    ],
    frameworks: [
      {
        id: 'governance',
        name: 'Governance alignment',
        description: 'Designed to support institutional oversight practices.',
      },
      {
        id: 'data',
        name: 'Data boundaries',
        description: 'Institution-scoped environments and clear ownership.',
      },
      {
        id: 'assurance',
        name: 'Assurance readiness',
        description: 'Evidence-friendly design for evaluation conversations.',
      },
    ],
  },
  pricingEntry: {
    title: 'Pricing and enterprise engagement',
    description:
      'Platform is evaluated with Command and Studio. Review packaging, or speak with AOVIAS about enterprise foundation needs.',
    primaryCta: {
      label: 'View pricing',
      href: '/pricing',
    },
    secondaryCta: {
      label: 'Contact enterprise',
      href: '/company/contact#form-enterprise',
    },
  },
  resources: {
    heading: 'Resources',
    description: 'Documentation and guidance for evaluating Platform.',
    items: [
      {
        id: 'plt-docs',
        kind: 'documentation',
        title: 'Platform orientation',
        description:
          'How shared foundation services relate to Command and Studio.',
        href: '/resources/docs',
        category: { id: 'docs', name: 'Documentation', tone: 'primary' },
      },
      {
        id: 'plt-arch',
        kind: 'article',
        title: 'Architecture principles',
        description:
          'Clear product boundaries with shared strength — not a monolith.',
        href: '/technology/architecture',
        category: { id: 'guides', name: 'Guides', tone: 'info' },
      },
      {
        id: 'plt-notes',
        kind: 'release-note',
        title: 'Platform notes',
        description: 'Measured updates as AOVIAS capabilities mature.',
        href: '/resources/library/platform-notes-2026-07',
        category: { id: 'releases', name: 'Release notes', tone: 'secondary' },
      },
    ],
  },
  primaryCtaBand: {
    title: 'Evaluate the AOVIAS foundation',
    description:
      'Request a demonstration of how Platform strengthens Command and Studio — or continue exploring the commercial portfolio.',
    primaryCta: {
      label: 'Request a demo',
      href: '/company/contact#form-demo',
    },
    secondaryCta: {
      label: 'Explore products',
      href: '/products',
    },
  },
} as const satisfies ProductDetailContent;
