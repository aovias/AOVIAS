import type { MarketingCta } from '@/components/marketing/types';
import type {
  ProductComparisonColumn,
  ProductComparisonRow,
} from '@/features/products/comparison/product-comparison-table';

export const overviewContent = {
  eyebrow: 'Products',
  title: 'Enterprise software for institutions that cannot pause.',
  description:
    'AOVIAS Command, Studio, and Platform are commercial products built on a shared foundation — distinct roles, one coherent system.',
  primaryCta: {
    label: 'Explore products',
    href: '#portfolio',
  } satisfies MarketingCta,
  secondaryCta: {
    label: 'Request a demo',
    href: '/company/contact#form-demo',
  } satisfies MarketingCta,
  portfolio: {
    eyebrow: 'Portfolio',
    heading: 'A focused commercial portfolio',
    description:
      'Three products. Clear jobs. Shared identity, security, deployment, AI, and governance — so organizations adopt capability without fragmenting their stack.',
  },
  together: {
    eyebrow: 'Ecosystem',
    heading: 'How they work together',
    description:
      'Command runs operations. Platform provides the shared foundation. Studio designs and publishes institutional work. Customers receive coherent outcomes — not disconnected tools.',
  },
  foundation: {
    eyebrow: 'Shared foundation',
    heading: 'Capabilities every product inherits',
    description:
      'Platform services strengthen Command and Studio without forcing a single undifferentiated application.',
    items: [
      {
        id: 'identity',
        heading: 'Identity',
        description:
          'Enterprise identity and role-aware access across the portfolio.',
      },
      {
        id: 'security',
        heading: 'Security',
        description:
          'Controls, boundaries, and auditability designed as product fundamentals.',
      },
      {
        id: 'deployment',
        heading: 'Deployment',
        description:
          'Institution-scoped environments with governed release pathways.',
      },
      {
        id: 'ai',
        heading: 'AI',
        description:
          'Assisted workflows with human accountability and clear governance.',
      },
      {
        id: 'governance',
        heading: 'Governance',
        description:
          'Policies, ownership, and evidence-friendly operational records.',
      },
    ],
  },
  comparison: {
    eyebrow: 'Compare',
    heading: 'Choose by job, not by feature checklist',
    description:
      'A practical matrix for evaluators. Purpose, users, capability, deployment, and outcomes — without marketing fluff.',
  },
  closing: {
    title: 'Evaluate AOVIAS with your team',
    description:
      'Explore the products in depth, or request a demonstration tailored to your institution.',
    primaryCta: {
      label: 'Explore products',
      href: '#portfolio',
    } satisfies MarketingCta,
    secondaryCta: {
      label: 'Request a demo',
      href: '/company/contact#form-demo',
    } satisfies MarketingCta,
  },
} as const;

export const portfolioProducts = [
  {
    id: 'command',
    name: 'AOVIAS Command',
    purpose:
      'Mission control for institutional operations — detect, investigate, approve, deploy, and observe with clear ownership.',
    audience:
      'Operations, reliability, and institutional control teams in healthcare, government, and enterprise environments.',
    primaryValue:
      'Operational confidence: visibility without noise, accountable workflows, continuity under pressure.',
    href: '/products/command',
    linkLabel: 'Explore Command',
    mediaLabel: 'AOVIAS Command — mission control dashboard',
  },
  {
    id: 'studio',
    name: 'AOVIAS Studio',
    purpose:
      'The authoring and publishing workspace where organizations define, design, standardize, publish, and improve.',
    audience:
      'Delivery, product, architecture, and institutional knowledge teams that need durable standards.',
    primaryValue:
      'Editorial precision: coherent models, governed publishing, versions that outlast release calendars.',
    href: '/products/studio',
    linkLabel: 'Explore Studio',
    mediaLabel: 'AOVIAS Studio — authoring workspace',
  },
  {
    id: 'platform',
    name: 'AOVIAS Platform',
    purpose:
      'The shared foundation that makes Command and Studio stronger together — identity, AI, deployment, security, and integration.',
    audience:
      'Platform, security, and enterprise architecture leaders responsible for a coherent institutional stack.',
    primaryValue:
      'Invisible strength: shared services that compound product value without forcing a monolith.',
    href: '/products/platform',
    linkLabel: 'Explore Platform',
    mediaLabel: 'AOVIAS Platform — services console',
  },
] as const;

export const comparisonColumns: readonly ProductComparisonColumn[] = [
  { id: 'command', name: 'Command' },
  { id: 'studio', name: 'Studio' },
  { id: 'platform', name: 'Platform' },
] as const;

export const comparisonRows: readonly ProductComparisonRow[] = [
  {
    id: 'purpose',
    label: 'Purpose',
    values: {
      command: 'Run institutional operations with accountable control',
      studio: 'Design, standardize, and publish institutional work',
      platform: 'Provide shared foundation services across products',
    },
  },
  {
    id: 'users',
    label: 'Users',
    values: {
      command: 'Operations, SRE, control-room, and duty teams',
      studio: 'Delivery, product, architecture, and knowledge teams',
      platform: 'Platform, security, and enterprise architecture',
    },
  },
  {
    id: 'capability',
    label: 'Core capability',
    values: {
      command: 'Incident and workflow execution with live operational state',
      studio: 'Modeling, review, publishing, and version stewardship',
      platform: 'Identity, policies, services, deployments, observability',
    },
  },
  {
    id: 'deployment',
    label: 'Deployment',
    values: {
      command: 'Institution-scoped operations environments',
      studio: 'Authoring spaces with governed release pathways',
      platform: 'Shared multi-region foundation services',
    },
  },
  {
    id: 'outcomes',
    label: 'Primary outcomes',
    values: {
      command: 'Faster resolution, clearer ownership, measurable continuity',
      studio: 'Consistent standards, durable handoffs, controlled change',
      platform: 'Coherent security posture and interoperable products',
    },
  },
] as const;
