import { getIndustryBySlug } from '@/features/industries/domain';
import { standardIndustryNav } from '@/features/industries/detail/shared';
import { defaultIndustryTrust } from '@/features/industries/detail/trust-defaults';
import type { IndustryDetailContent } from '@/features/industries/detail/types';

const industry = getIndustryBySlug('government');

export const governmentDetailContent = {
  slug: 'government',
  name: industry?.name ?? 'Government',
  status: 'available',
  seo: {
    title: 'Government',
    description:
      'AOVIAS for government — accountable operations and durable platforms for public programs that must withstand oversight and long program lifecycles.',
    keywords: [
      'AOVIAS government',
      'public sector platforms',
      'government program technology',
      'civic institutional software',
    ],
  },
  navigation: standardIndustryNav,
  hero: {
    eyebrow: 'Government',
    headline: 'Systems public institutions can stand behind',
    challengesSummary:
      'Public programs need technology that survives scrutiny, procurement cycles, and leadership transitions — not tools optimized for novelty.',
    primaryCta: { label: 'Request a demo', href: '/company/contact#form-demo' },
    secondaryCta: { label: 'Explore products', href: '/products' },
    illustrationLabel: 'Government — transparency and continuity',
  },
  overview: {
    heading: 'Institutional context',
    description:
      'Agencies and public programs operate under civic duty. Decisions affect constituents; systems must be explainable to leadership, auditors, and the public. Continuity matters more than release theater — knowledge and operations must outlast individual teams.',
    opportunities:
      'Modernization succeeds when platforms reduce ambiguity in program delivery, preserve attributable control, and remain operable across multi-year funding and policy cycles. AOVIAS Command anchors operational accountability; AOVIAS Studio supports coherent delivery. Interoperability and assurance products extend that foundation as program portfolios grow.',
  },
  challenges: {
    heading: 'Government challenges',
    description:
      'Structural pressures that shape technology choices in public programs.',
    items: [
      {
        id: 'scrutiny',
        title: 'Decisions under public scrutiny',
        description:
          'Work and access patterns must withstand review. Opaque tooling undermines confidence even when day-to-day operations appear intact.',
      },
      {
        id: 'legacy',
        title: 'Programs spanning aging systems',
        description:
          'Critical services often depend on layered integrations with unclear ownership, making change costly and risk hard to localize.',
      },
      {
        id: 'transitions',
        title: 'Continuity across administrations',
        description:
          'Staffing, priorities, and contractors shift; institutional knowledge and operational control must not degrade quietly between transitions.',
      },
      {
        id: 'procurement',
        title: 'Procurement that favors durability',
        description:
          'Agencies need capability they can sustain and explain — not vendor narratives that outpace delivery or lock institutions into brittle paths.',
      },
    ],
  },
  solutions: {
    heading: 'How AOVIAS helps',
    description:
      'AOVIAS emphasizes accountable operations, coherent delivery standards, and long-term stewardship suited to public programs.',
    items: [
      {
        id: 'command',
        heading: 'AOVIAS Command for program operations',
        description:
          'Command supports structured visibility and control where public services depend on reliable, attributable operational work.',
      },
      {
        id: 'studio',
        heading: 'AOVIAS Studio for governed delivery',
        description:
          'Studio helps teams design and ship institutional solutions with shared standards that survive contractor and team turnover.',
      },
      {
        id: 'assurance',
        heading: 'Interoperability and assurance posture',
        description:
          'Nexus and Sentinel are positioned for governed connectivity and institutional vigilance as portfolios mature — without uncontrolled sprawl.',
      },
    ],
  },
  products: {
    heading: 'Recommended products',
    description:
      'Products that strengthen accountability, delivery coherence, interoperability, and assurance for public programs.',
    productIds: industry?.recommendedProductIds ?? ['command', 'studio'],
  },
  benefits: {
    heading: 'Organizational outcomes',
    description:
      'Practical results public institutions can evaluate under oversight — not modernization slogans.',
    items: [
      {
        id: 'explainability',
        heading: 'Explainable operations',
        description:
          'Status, ownership, and escalations can be described clearly in leadership and audit conversations.',
      },
      {
        id: 'program-continuity',
        heading: 'Program continuity',
        description:
          'Operational knowledge and control patterns are less dependent on a single team or vendor engagement.',
      },
      {
        id: 'delivery',
        heading: 'Coherent delivery standards',
        description:
          'Solutions share structure so new work does not quietly fragment the institutional landscape.',
      },
      {
        id: 'oversight',
        heading: 'Oversight-ready design',
        description:
          'Systems are shaped for evidence and review rather than after-the-fact reconstruction.',
      },
      {
        id: 'service',
        heading: 'More consistent public service',
        description:
          'Programs the public relies on see fewer gaps caused by unclear handoffs and opaque tooling.',
      },
    ],
  },
  trust: {
    ...defaultIndustryTrust,
    description:
      'Public-sector trust requires restraint in claims, durable engineering, and systems that can be inspected — without invented certifications.',
  },
  resources: {
    heading: 'Resources',
    description: 'Documentation and guidance for government evaluation.',
    items: [
      {
        id: 'gov-docs',
        kind: 'documentation',
        title: 'Platform overview',
        description:
          'How the AOVIAS platform supports accountable institutional programs.',
        href: '/resources/docs/platform-overview',
        category: { id: 'docs', name: 'Documentation', tone: 'primary' },
      },
      {
        id: 'gov-guide',
        kind: 'article',
        title: 'Institutional trust in public technology',
        description:
          'Why continuity, restraint, and ownership matter for systems society depends on.',
        href: '/resources/blog/institutional-trust',
        category: { id: 'blog', name: 'Articles', tone: 'info' },
      },
      {
        id: 'gov-eval',
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
    title: 'Discuss government needs with AOVIAS',
    description:
      'Request a demonstration, contact enterprise sales, or explore products suited to public institutions.',
    primaryCta: { label: 'Request a demo', href: '/company/contact#form-demo' },
    secondaryCta: { label: 'Contact enterprise', href: '/company/contact#form-enterprise' },
  },
} as const satisfies IndustryDetailContent;
