import { getProductBySlug } from '@/features/products/domain';
import type { ProductDetailContent } from '@/features/products/detail/types';
import {
  productMedia,
  standardProductNav,
} from '@/features/products/detail/shared';

const product = getProductBySlug('command');

/**
 * AOVIAS Command — mission control product detail.
 */
export const commandDetailContent = {
  slug: 'command',
  status: 'available',
  name: product?.name ?? 'AOVIAS Command',
  tagline:
    'Mission control for institutions that cannot afford ambiguity.',
  description:
    product?.description ??
    'Detect, investigate, approve, deploy, and observe — with operational confidence.',
  badge: 'Available',
  atmosphere: 'enterprise',
  ...(product?.highlights ? { highlights: product.highlights } : {}),
  heroMedia: productMedia('AOVIAS Command — mission control dashboard'),
  secondaryHeroMedia: productMedia('Command operational workflow'),
  primaryCta: {
    label: 'Request a demo',
    href: '/company/contact#form-demo',
  },
  secondaryCta: {
    label: 'Explore pricing',
    href: '/pricing',
  },
  seo: {
    title: 'AOVIAS Command — Enterprise Operations Mission Control',
    description:
      'AOVIAS Command is mission control for institutional operations — live visibility, accountable workflows, and execution where continuity matters.',
    keywords: [
      'AOVIAS Command',
      'enterprise operations',
      'mission control software',
      'operational command center',
    ],
  },
  navigation: standardProductNav,
  overview: {
    heading: 'Operational story',
    description:
      'Critical operations fragment across tools that favor attention over accountability. Teams lose ownership at handoffs, visibility becomes noise, and institutional duty is harder to discharge under pressure.',
    audience:
      'Command is mission control for healthcare, government, and enterprise operations leaders — detect, investigate, approve, deploy, and observe with clear ownership. Not compliance software. Not dashboards designed for spectacle.',
    media: productMedia('Command mission control overview'),
    metrics: [
      { id: 'visibility', value: 'Visibility', label: 'Operational clarity' },
      { id: 'ownership', value: 'Ownership', label: 'Accountable workflows' },
      { id: 'continuity', value: 'Continuity', label: 'Institutional operations' },
    ],
  },
  capabilities: {
    heading: 'Key workflows',
    description:
      'Command concentrates on operational judgment: what to see, who owns the next action, and how work remains inspectable from detection through observation.',
    showcases: [
      {
        id: 'dashboard',
        title: 'Dashboard as the visual centerpiece',
        description:
          'A production operations surface — queues, metrics, incidents, and state — designed to feel desirable and reliable under pressure.',
        media: productMedia('Command mission control dashboard'),
        align: 'end',
      },
      {
        id: 'workflow',
        title: 'Detect → Investigate → Approve → Deploy → Observe',
        description:
          'A durable operational path with ownership at every step. Escalations, handoffs, and closures preserve auditability across teams and shifts.',
        media: productMedia('Command detect investigate workflow'),
        align: 'start',
      },
      {
        id: 'intelligence',
        title: 'Operational intelligence without noise',
        description:
          'Structure signals so operators can prioritize judgment. Surfaces emphasize ownership and state — not endless undifferentiated alerts.',
        media: productMedia('Command operational intelligence'),
        align: 'end',
      },
      {
        id: 'integrations',
        title: 'Platform integrations that stay governed',
        description:
          'Connect Command to identity, records, notifications, and observability with clear ownership of data and alerts — on the AOVIAS Platform foundation.',
        media: productMedia('Command platform integrations workflow'),
        align: 'start',
      },
    ],
    summaryItems: [
      {
        id: 'workflows',
        title: 'Critical workflows',
        description:
          'Model complex operational paths with durable ownership and review points.',
        category: 'Operations',
      },
      {
        id: 'escalation',
        title: 'Accountable escalation',
        description: 'Handoffs that remain inspectable and attributable.',
        category: 'Governance',
      },
      {
        id: 'continuity',
        title: 'Operational continuity',
        description:
          'Change discipline and resilience patterns for long-lived institutional duty.',
        category: 'Quality',
      },
    ],
  },
  architecture: {
    title: 'Architecture and platform relationship',
    description:
      'Command is the operations plane on the AOVIAS Platform foundation — shared identity, security boundaries, and interoperability standards, with a clear boundary for operational work.',
    diagram: productMedia('Command architecture diagram'),
    caption:
      'Command sits beside Studio on a shared institutional platform — operations and delivery, not a single undifferentiated tool.',
  },
  integrations: {
    heading: 'Platform integrations',
    description:
      'Connect Command to institutional systems with governed boundaries and clear ownership of data and alerts.',
    items: [
      {
        id: 'identity',
        name: 'Identity providers',
        description: 'Enterprise identity foundations for role-aware access',
        category: 'Security',
      },
      {
        id: 'records',
        name: 'Records systems',
        description: 'Institutional source systems with purpose-bound use',
        category: 'Data',
      },
      {
        id: 'notify',
        name: 'Notification channels',
        description: 'Operational alerts and handoffs operators can trust',
        category: 'Operations',
      },
      {
        id: 'observability',
        name: 'Observability tools',
        description: 'Signals that support operational judgment',
        category: 'Platform',
      },
    ],
  },
  specifications: {
    heading: 'Evaluation dimensions',
    description:
      'Categories institutions typically assess when evaluating an operations workspace.',
    categories: [
      {
        id: 'platform',
        title: 'Platform',
        rows: [
          {
            id: 'role',
            label: 'Product role',
            value: 'Institutional operations workspace',
          },
          {
            id: 'ecosystem',
            label: 'Platform relationship',
            value: 'AOVIAS platform — operations plane',
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
        id: 'apis',
        title: 'APIs',
        rows: [
          {
            id: 'integration',
            label: 'Integration style',
            value: 'Governed platform APIs',
          },
          {
            id: 'events',
            label: 'Events',
            value: 'Operational event pathways',
          },
        ],
      },
      {
        id: 'environments',
        title: 'Supported environments',
        rows: [
          { id: 'enterprise', label: 'Enterprise', value: 'Supported' },
          {
            id: 'regulated',
            label: 'Regulated contexts',
            value: 'Designed for institutional duty',
          },
        ],
      },
    ],
  },
  trust: {
    heading: 'Trust and security',
    description:
      'Security, reliability, and enterprise readiness are product fundamentals — not afterthoughts.',
    signals: [
      {
        id: 'security',
        label: 'Security-minded',
        description: 'Controls as product fundamentals',
        variant: 'security',
      },
      {
        id: 'reliability',
        label: 'Reliability',
        description: 'Built for continuity',
        variant: 'quality',
      },
      {
        id: 'enterprise',
        label: 'Enterprise ready',
        description: 'Institutional constraints respected',
        variant: 'compliance',
      },
    ],
    highlights: [
      'Access and actions designed for auditability',
      'Clear ownership across operational handoffs',
      'Measured claims — no artificial urgency',
    ],
    controls: [
      {
        id: 'access',
        title: 'Access discipline',
        description: 'Identity-aware access aligned to institutional roles.',
        category: 'Security',
      },
      {
        id: 'audit',
        title: 'Traceability',
        description: 'Operational actions remain attributable over time.',
        category: 'Governance',
      },
      {
        id: 'resilience',
        title: 'Operational resilience',
        description: 'Patterns that favor continuity under pressure.',
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
        description: 'Institution-scoped environments and clear data ownership.',
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
      'Review published packaging for institutional teams, begin checkout when a plan fits, or speak with AOVIAS about enterprise needs and implementation.',
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
    description: 'Documentation and guidance for evaluating Command.',
    items: [
      {
        id: 'cmd-docs',
        kind: 'documentation',
        title: 'Command getting started',
        description:
          'Orientation for teams evaluating Command in institutional environments.',
        href: '/resources/docs/command-getting-started',
        category: { id: 'docs', name: 'Documentation', tone: 'primary' },
      },
      {
        id: 'cmd-guide',
        kind: 'article',
        title: 'Designing for institutional trust',
        description:
          'How clarity and accountability shape systems operators can rely on.',
        href: '/resources/blog/institutional-trust',
        category: { id: 'guides', name: 'Guides', tone: 'info' },
      },
      {
        id: 'cmd-notes',
        kind: 'release-note',
        title: 'Platform notes',
        description: 'Measured updates as AOVIAS capabilities mature.',
        href: '/resources/library/platform-notes-2026-07',
        category: { id: 'releases', name: 'Release notes', tone: 'secondary' },
      },
    ],
  },
  primaryCtaBand: {
    title: 'See Command in your environment',
    description:
      'Request a demonstration of mission-control operations — or review pricing for institutional teams.',
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
