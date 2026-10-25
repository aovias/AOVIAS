import { getProductBySlug } from '@/features/products/domain';
import type { ProductDetailContent } from '@/features/products/detail/types';
import {
  productMedia,
  standardProductNav,
} from '@/features/products/detail/shared';

const product = getProductBySlug('studio');

/**
 * AOVIAS Studio — editorial authoring product detail.
 */
export const studioDetailContent = {
  slug: 'studio',
  status: 'available',
  name: product?.name ?? 'AOVIAS Studio',
  tagline: 'Where organizations define, design, standardize, publish, and improve.',
  description:
    product?.description ??
    'An editorial workspace for institutional modeling — precise, structured, and built to endure.',
  badge: 'Available',
  atmosphere: 'knowledge',
  ...(product?.highlights ? { highlights: product.highlights } : {}),
  heroMedia: productMedia('AOVIAS Studio — authoring workspace'),
  secondaryHeroMedia: productMedia('Studio publishing lifecycle workflow'),
  primaryCta: {
    label: 'Request a demo',
    href: '/company/contact#form-demo',
  },
  secondaryCta: {
    label: 'Explore pricing',
    href: '/pricing',
  },
  seo: {
    title: 'AOVIAS Studio — Institutional Authoring & Publishing',
    description:
      'AOVIAS Studio is the editorial workspace for institutional teams — model, review, publish, version, and release with durable standards.',
    keywords: [
      'AOVIAS Studio',
      'institutional authoring',
      'enterprise publishing workspace',
      'design standards software',
    ],
  },
  navigation: standardProductNav,
  overview: {
    heading: 'An editorial workspace — not another dashboard',
    description:
      'Institutional solutions fragment across tools, teams, and handoffs. Standards drift, context is lost at transitions, and delivery quality depends on who happened to be in the room.',
    audience:
      'Studio is for delivery, product, architecture, and knowledge teams that need a precise place to define and publish coherent work — warmer and more structured than an operations console, more disciplined than a document dump.',
    media: productMedia('Studio authoring workspace overview'),
    metrics: [
      { id: 'consistency', value: 'Standards', label: 'Shared team practice' },
      { id: 'delivery', value: 'Publish', label: 'Governed release' },
      { id: 'stewardship', value: 'Versions', label: 'Long-lived solutions' },
    ],
  },
  capabilities: {
    heading: 'Authoring and publishing',
    description:
      'Studio concentrates on how institutional work is modeled, reviewed, published, versioned, and released with integrity.',
    showcases: [
      {
        id: 'workspace',
        title: 'Workspace preview',
        description:
          'A structured authoring surface — library, models, standards, and publish actions — designed to feel editorial and precise.',
        media: productMedia('Studio authoring workspace'),
        align: 'end',
      },
      {
        id: 'lifecycle',
        title: 'Model → Review → Publish → Version → Release',
        description:
          'A clear publishing lifecycle so institutional knowledge survives transitions and remains operable by the next steward.',
        media: productMedia('Studio publishing lifecycle workflow'),
        align: 'start',
      },
      {
        id: 'standards',
        title: 'Design standards that travel',
        description:
          'Keep quality and patterns coherent across people, projects, and time — reducing silent drift as teams grow.',
        media: productMedia('Studio design standards capability'),
        align: 'end',
      },
      {
        id: 'collaboration',
        title: 'Collaboration with ownership',
        description:
          'Review and handoff patterns that preserve intent — so critique improves the work without erasing accountability.',
        media: productMedia('Studio collaboration handoff workflow'),
        align: 'start',
      },
    ],
    summaryItems: [
      {
        id: 'workspace',
        title: 'Authoring workspace',
        description: 'A coherent place for modeling and publishing work.',
        category: 'Delivery',
      },
      {
        id: 'quality',
        title: 'Design standards',
        description: 'Shared expectations that improve consistency.',
        category: 'Quality',
      },
      {
        id: 'continuity',
        title: 'Version stewardship',
        description: 'Built for solutions that outlast release calendars.',
        category: 'Continuity',
      },
    ],
  },
  architecture: {
    title: 'Architecture and platform relationship',
    description:
      'Studio is the delivery plane on the AOVIAS Platform foundation — aligned with shared security, identity, and interoperability — beside Command’s operations plane.',
    diagram: productMedia('Studio architecture diagram'),
    caption:
      'Studio and Command share platform foundations while preserving distinct jobs: authoring versus operations.',
  },
  integrations: {
    heading: 'Collaboration and systems',
    description:
      'Connect Studio to the systems and repositories institutions already trust — with governed boundaries.',
    items: [
      {
        id: 'repos',
        name: 'Source repositories',
        description: 'Delivery history and versioned work',
        category: 'Engineering',
      },
      {
        id: 'identity',
        name: 'Identity providers',
        description: 'Enterprise access control',
        category: 'Security',
      },
      {
        id: 'knowledge',
        name: 'Knowledge bases',
        description: 'Documentation and guidance',
        category: 'Content',
      },
      {
        id: 'ci',
        name: 'Delivery pipelines',
        description: 'Governed release pathways',
        category: 'Platform',
      },
    ],
  },
  specifications: {
    heading: 'Evaluation dimensions',
    description:
      'Categories institutions typically assess when evaluating a delivery workspace.',
    categories: [
      {
        id: 'platform',
        title: 'Platform',
        rows: [
          {
            id: 'role',
            label: 'Product role',
            value: 'Institutional delivery workspace',
          },
          {
            id: 'ecosystem',
            label: 'Platform relationship',
            value: 'AOVIAS platform — delivery plane',
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
            value: 'Enterprise deployment options',
          },
          {
            id: 'tenancy',
            label: 'Tenancy',
            value: 'Team- and institution-scoped workspaces',
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
            id: 'roles',
            label: 'Collaboration',
            value: 'Role-aware contribution',
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
            value: 'Governed platform APIs',
          },
          {
            id: 'content',
            label: 'Content exchange',
            value: 'Documentation-friendly pathways',
          },
        ],
      },
      {
        id: 'environments',
        title: 'Supported environments',
        rows: [
          { id: 'enterprise', label: 'Enterprise', value: 'Supported' },
          {
            id: 'education',
            label: 'Education & research',
            value: 'Designed for institutional delivery',
          },
        ],
      },
    ],
  },
  trust: {
    heading: 'Trust and security',
    description:
      'Studio treats security, reliability, and accountable collaboration as core delivery requirements.',
    signals: [
      {
        id: 'security',
        label: 'Security-minded',
        description: 'Access aligned to institutional roles',
        variant: 'security',
      },
      {
        id: 'quality',
        label: 'Engineering quality',
        description: 'Consistency as a product requirement',
        variant: 'quality',
      },
      {
        id: 'enterprise',
        label: 'Enterprise ready',
        description: 'Built for long-lived teams',
        variant: 'compliance',
      },
    ],
    highlights: [
      'Role-aware collaboration and contribution',
      'Institution-scoped workspace boundaries',
      'Delivery practices that favor continuity',
    ],
    controls: [
      {
        id: 'access',
        title: 'Access control',
        description: 'Enterprise identity with clear contribution boundaries.',
        category: 'Security',
      },
      {
        id: 'integrity',
        title: 'Work integrity',
        description: 'Shared standards that reduce silent quality drift.',
        category: 'Quality',
      },
      {
        id: 'stewardship',
        title: 'Stewardship',
        description: 'Handoffs that preserve context for the next owner.',
        category: 'Continuity',
      },
    ],
    frameworks: [
      {
        id: 'governance',
        name: 'Delivery governance',
        description: 'Supports oversight of how institutional solutions are built.',
      },
      {
        id: 'data',
        name: 'Workspace boundaries',
        description: 'Clear separation aligned to institutional ownership.',
      },
      {
        id: 'assurance',
        name: 'Evaluation readiness',
        description: 'Transparent structure for enterprise assessment.',
      },
    ],
  },
  pricingEntry: {
    title: 'Pricing and enterprise engagement',
    description:
      'Review published packaging for delivery teams, begin checkout when a plan fits, or speak with AOVIAS about enterprise adoption and implementation.',
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
    description: 'Documentation and guidance for evaluating Studio.',
    items: [
      {
        id: 'stu-docs',
        kind: 'documentation',
        title: 'Studio delivery documentation',
        description:
          'Orientation for teams evaluating Studio in institutional delivery practice.',
        href: '/resources/docs/studio-delivery',
        category: { id: 'docs', name: 'Documentation', tone: 'primary' },
      },
      {
        id: 'stu-guide',
        kind: 'article',
        title: 'Release discipline',
        description:
          'Practices for consistent stewardship of institutional software.',
        href: '/resources/blog/release-discipline',
        category: { id: 'guides', name: 'Guides', tone: 'info' },
      },
      {
        id: 'stu-notes',
        kind: 'release-note',
        title: 'Platform notes',
        description: 'Measured updates as AOVIAS capabilities mature.',
        href: '/resources/library/platform-notes-2026-07',
        category: { id: 'releases', name: 'Release notes', tone: 'secondary' },
      },
    ],
  },
  primaryCtaBand: {
    title: 'See Studio with your standards',
    description:
      'Request a demonstration of authoring and publishing — or review pricing for institutional delivery teams.',
    primaryCta: {
      label: 'Request a demo',
      href: '/company/contact#form-demo',
    },
    secondaryCta: {
      label: 'Explore pricing',
      href: '/pricing',
    },
  },
} as const satisfies ProductDetailContent;
