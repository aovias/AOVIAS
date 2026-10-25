import { getIndustryBySlug } from '@/features/industries/domain';
import { standardIndustryNav } from '@/features/industries/detail/shared';
import { defaultIndustryTrust } from '@/features/industries/detail/trust-defaults';
import type { IndustryDetailContent } from '@/features/industries/detail/types';

const industry = getIndustryBySlug('enterprise');

export const enterpriseDetailContent = {
  slug: 'enterprise',
  name: industry?.name ?? 'Enterprise',
  status: 'available',
  seo: {
    title: 'Enterprise',
    description:
      'AOVIAS for enterprise — operational command and disciplined delivery for large organizations that need governance, security, and lasting platform value.',
    keywords: [
      'AOVIAS enterprise',
      'enterprise operations platforms',
      'enterprise delivery governance',
      'institutional enterprise software',
    ],
  },
  navigation: standardIndustryNav,
  hero: {
    eyebrow: 'Enterprise',
    headline: 'Enterprise platforms built for lasting responsibility',
    challengesSummary:
      'Large organizations need coherent systems that preserve ownership across business units, reduce operational risk, and endure beyond release cycles.',
    primaryCta: { label: 'Request a demo', href: '/company/contact#form-demo' },
    secondaryCta: { label: 'Explore products', href: '/products' },
    illustrationLabel: 'Enterprise — governance and scale',
  },
  overview: {
    heading: 'Institutional context',
    description:
      'Enterprises run portfolios of products, shared services, and regulated workflows. Security and governance determine whether technology creates lasting value; short-cycle launches that ignore ownership create cost that compounds across business units.',
    opportunities:
      'Platform investments succeed when they reduce tool sprawl, clarify operational control, and give delivery teams shared standards. AOVIAS Command and AOVIAS Studio form that foundation. Nexus and Sentinel extend interoperability and assurance as the enterprise portfolio matures — without committing to invented timelines or certifications.',
  },
  challenges: {
    heading: 'Enterprise challenges',
    description:
      'Structural pressures common when organizations operate at scale across teams and portfolios.',
    items: [
      {
        id: 'sprawl',
        title: 'Portfolio and tool sprawl',
        description:
          'Business units adopt disconnected systems that raise cost, obscure accountability, and make enterprise standards hard to enforce.',
      },
      {
        id: 'control',
        title: 'Control gaps in critical workflows',
        description:
          'High-value operations lack consistent visibility, escalation paths, and auditability across shared services and line-of-business teams.',
      },
      {
        id: 'governance-lag',
        title: 'Capability ahead of governance',
        description:
          'New tools and automation arrive faster than the organization can standardize access, ownership, and sustainment.',
      },
      {
        id: 'short-cycle',
        title: 'Short-cycle value traps',
        description:
          'Initiatives optimize for launch moments rather than durable institutional return, leaving successors to inherit undocumented systems.',
      },
    ],
  },
  solutions: {
    heading: 'How AOVIAS helps',
    description:
      'AOVIAS provides a coherent platform foundation — operational command, disciplined delivery, and governed growth of the enterprise portfolio.',
    items: [
      {
        id: 'command',
        heading: 'AOVIAS Command for enterprise operations',
        description:
          'Command strengthens visibility and attributable control where reliability and escalation matter across shared services and critical workflows.',
      },
      {
        id: 'studio',
        heading: 'AOVIAS Studio for delivery discipline',
        description:
          'Studio helps teams ship institutional solutions with shared standards so delivery does not silently fragment by business unit.',
      },
      {
        id: 'portfolio',
        heading: 'Governed portfolio extension',
        description:
          'Nexus and Sentinel extend interoperability and assurance as the organization connects systems and deepens oversight — on institutional terms.',
      },
    ],
  },
  products: {
    heading: 'Recommended products',
    description:
      'A focused set of platforms for enterprise operations, delivery governance, interoperability, and assurance.',
    productIds: industry?.recommendedProductIds ?? ['command', 'studio'],
  },
  benefits: {
    heading: 'Organizational outcomes',
    description:
      'Outcomes large organizations can evaluate in governance, operations, and delivery — without empty transformation language.',
    items: [
      {
        id: 'ownership',
        heading: 'Enterprise ownership clarity',
        description:
          'Platforms, escalations, and delivery work carry named responsibility across units and shared services.',
      },
      {
        id: 'efficiency',
        heading: 'Less reconciling across tools',
        description:
          'Operators and leaders spend less time reconstructing status from fragmented systems.',
      },
      {
        id: 'standards',
        heading: 'Shared delivery standards',
        description:
          'Teams reduce silent drift by designing and shipping against common institutional patterns.',
      },
      {
        id: 'posture',
        heading: 'Stronger evaluation posture',
        description:
          'Systems are shaped for auditability and enterprise review without exaggerated security claims.',
      },
      {
        id: 'durability',
        heading: 'Longer platform return',
        description:
          'Investments favor operability and stewardship over launch moments that leave successors stranded.',
      },
    ],
  },
  trust: {
    ...defaultIndustryTrust,
    description:
      'Enterprise readiness means security, reliability, and accountable platform growth — without exaggerated claims or invented certifications.',
  },
  resources: {
    heading: 'Resources',
    description: 'Documentation and guidance for enterprise evaluation.',
    items: [
      {
        id: 'ent-docs',
        kind: 'documentation',
        title: 'Platform overview',
        description:
          'How the AOVIAS platform supports large institutional organizations.',
        href: '/resources/docs/platform-overview',
        category: { id: 'docs', name: 'Documentation', tone: 'primary' },
      },
      {
        id: 'ent-guide',
        kind: 'insight',
        title: 'Responsible AI',
        description:
          'How AOVIAS approaches intelligence with restraint and accountable judgment.',
        href: '/resources/insights/responsible-ai',
        category: { id: 'insights', name: 'Insights', tone: 'info' },
      },
      {
        id: 'ent-eval',
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
    title: 'Engage AOVIAS for enterprise needs',
    description:
      'Request a demonstration, contact enterprise sales, or explore the product portfolio.',
    primaryCta: { label: 'Request a demo', href: '/company/contact#form-demo' },
    secondaryCta: { label: 'Contact enterprise', href: '/company/contact#form-enterprise' },
  },
} as const satisfies IndustryDetailContent;
