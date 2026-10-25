import { getIndustryBySlug } from '@/features/industries/domain';
import { standardIndustryNav } from '@/features/industries/detail/shared';
import { defaultIndustryTrust } from '@/features/industries/detail/trust-defaults';
import type { IndustryDetailContent } from '@/features/industries/detail/types';

const industry = getIndustryBySlug('healthcare');

export const healthcareDetailContent = {
  slug: 'healthcare',
  name: industry?.name ?? 'Healthcare',
  status: 'available',
  seo: {
    title: 'Healthcare',
    description:
      'AOVIAS for healthcare — operational clarity and disciplined delivery for care environments where continuity and accountable judgment matter.',
    keywords: [
      'AOVIAS healthcare',
      'healthcare operations technology',
      'clinical operations platforms',
      'care institution software',
    ],
  },
  navigation: standardIndustryNav,
  hero: {
    eyebrow: 'Healthcare',
    headline: 'Technology worthy of care environments',
    challengesSummary:
      'Care delivery depends on clear handoffs, attributable decisions, and systems that remain operable when clinical and operational pressure peaks.',
    primaryCta: { label: 'Request a demo', href: '/company/contact#form-demo' },
    secondaryCta: { label: 'Explore products', href: '/products' },
    illustrationLabel: 'Healthcare — care, compliance, continuity',
  },
  overview: {
    heading: 'Institutional context',
    description:
      'Hospitals, health systems, and care networks carry work that cannot tolerate opaque tools. Clinical and operational teams share responsibility for patient-facing continuity, while leadership must explain access, escalation, and change with precision.',
    opportunities:
      'As care settings digitize scheduling, quality review, and cross-unit coordination, institutions that preserve ownership and evidence trails can adopt new capability without weakening clinical trust. AOVIAS Command and AOVIAS Studio are positioned for that path — clarity in operations, discipline in delivery — with room for accountable insight and workflow products as needs mature.',
  },
  challenges: {
    heading: 'Healthcare challenges',
    description:
      'Pressures that arise where clinical duty and institutional operations meet.',
    items: [
      {
        id: 'handoffs',
        title: 'Handoffs across care and operations',
        description:
          'Status and ownership blur when clinical units, support services, and administrative teams work from disconnected views of the same work.',
      },
      {
        id: 'evidence',
        title: 'Evidence under review',
        description:
          'Quality, privacy, and assurance conversations require systems that show who acted, what changed, and why — without inventing theater around controls.',
      },
      {
        id: 'uptime',
        title: 'Continuity when demand spikes',
        description:
          'Care environments absorb surge, staffing change, and incident response; brittle process tooling becomes risk precisely when reliability matters most.',
      },
      {
        id: 'adoption',
        title: 'Capability without clinical burden',
        description:
          'New software arrives faster than training and governance can absorb it, leaving operators to reconcile tools instead of focusing on care coordination.',
      },
    ],
  },
  solutions: {
    heading: 'How AOVIAS helps',
    description:
      'AOVIAS strengthens care institutions through operational control and delivery discipline — with human accountability kept visible.',
    items: [
      {
        id: 'command',
        heading: 'AOVIAS Command for care operations',
        description:
          'Command gives operations teams structured visibility and attributable control over critical institutional work spanning units and shifts.',
      },
      {
        id: 'studio',
        heading: 'AOVIAS Studio for lasting improvements',
        description:
          'Studio helps teams design and ship operational and clinical-adjacent solutions with shared standards so improvements endure beyond a single project.',
      },
      {
        id: 'accountable',
        heading: 'Accountable capability growth',
        description:
          'Future insight and workflow products are framed as decision support under oversight — not opaque automation in environments that require judgment.',
      },
    ],
  },
  products: {
    heading: 'Recommended products',
    description:
      'Begin with Command for operational clarity and Studio for disciplined delivery; expand to insight and flow products as institutional maturity allows.',
    productIds: industry?.recommendedProductIds ?? ['command', 'studio'],
  },
  benefits: {
    heading: 'Organizational outcomes',
    description:
      'Results care institutions can evaluate in operations and oversight — not transformation slogans.',
    items: [
      {
        id: 'ownership',
        heading: 'Clearer operational ownership',
        description:
          'Handoffs and escalations carry named responsibility across clinical support and administrative teams.',
      },
      {
        id: 'consistency',
        heading: 'More consistent practice',
        description:
          'Shared delivery standards reduce one-off process tools that drift under staffing change.',
      },
      {
        id: 'review',
        heading: 'Review-ready work trails',
        description:
          'Actions and status are easier to explain in quality, privacy, and leadership review.',
      },
      {
        id: 'focus',
        heading: 'Less reconciliation burden',
        description:
          'Operators spend less time stitching status across disconnected systems during peak demand.',
      },
      {
        id: 'stewardship',
        heading: 'Stewardship across change',
        description:
          'New capability can be introduced with clearer training, ownership, and sustainment paths.',
      },
    ],
  },
  trust: {
    ...defaultIndustryTrust,
    description:
      'Care environments require security, reliability, and measured claims. AOVIAS treats those as product fundamentals — without inventing certifications or guarantees.',
  },
  resources: {
    heading: 'Resources',
    description: 'Guides and documentation for healthcare evaluation.',
    items: [
      {
        id: 'hc-docs',
        kind: 'documentation',
        title: 'Command getting started',
        description:
          'How teams begin with AOVIAS Command for operational clarity in institutional settings.',
        href: '/resources/docs/command-getting-started',
        category: { id: 'docs', name: 'Documentation', tone: 'primary' },
      },
      {
        id: 'hc-guide',
        kind: 'insight',
        title: 'Operational clarity as institutional advantage',
        description:
          'Principles for accountable visibility where continuity and judgment matter.',
        href: '/resources/insights/operational-clarity',
        category: { id: 'insights', name: 'Insights', tone: 'info' },
      },
      {
        id: 'hc-eval',
        kind: 'resource',
        title: 'Evaluation checklist',
        description:
          'A practical checklist for institutions assessing AOVIAS platform fit.',
        href: '/resources/library/evaluation-checklist',
        category: { id: 'library', name: 'Library', tone: 'secondary' },
      },
    ],
  },
  primaryCtaBand: {
    title: 'Talk about healthcare with AOVIAS',
    description:
      'Request a demonstration to evaluate fit for your care institution, or explore the product portfolio.',
    primaryCta: { label: 'Request a demo', href: '/company/contact#form-demo' },
    secondaryCta: { label: 'Explore products', href: '/products' },
  },
} as const satisfies IndustryDetailContent;
