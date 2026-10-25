import type { IndustryEditorialContent } from '@/features/industries/editorial/types';

/** Government — civic, stable, transparent institutional narrative. */
export const governmentEditorialContent = {
  slug: 'government',
  name: 'Government',
  atmosphere: 'blueprint-grid',
  domainVariant: 'government',
  environmentVariant: 'architecture',
  seo: {
    title: 'Government — Accountable Public Operations Platform',
    description:
      'AOVIAS for government: public accountability, citizen services, and operational resilience on a shared enterprise platform.',
    keywords: [
      'AOVIAS government',
      'public sector operations platform',
      'government digital services',
      'civic technology governance',
    ],
  },
  hero: {
    eyebrow: 'Government',
    title: 'Public accountability that outlasts administrations.',
    description:
      'Agencies need systems that can be explained under scrutiny, sustained across leadership cycles, and operated without opaque tooling.',
    primaryCta: {
      label: 'Request a demo',
      href: '/company/contact#form-demo',
    },
    secondaryCta: {
      label: 'Talk to our team',
      href: '/company/contact',
    },
    illustrationCaption: 'Accountability · services · resilience',
  },
  reality: {
    title: 'The public-sector operating environment',
    description:
      'Government programs carry lasting responsibility to citizens. Transparency, service delivery, and operational resilience are institutional duties — not optional features.',
    pillars: [
      'Public accountability',
      'Transparency',
      'Citizen services',
      'Operational resilience',
    ],
  },
  challenges: {
    title: 'Operational challenges in public institutions',
    description:
      'Pressures that arise where civic duty and complex program delivery meet.',
    items: [
      {
        id: 'scrutiny',
        title: 'Explainability under scrutiny',
        description:
          'Decisions and changes must remain attributable across departments and contractors.',
      },
      {
        id: 'continuity',
        title: 'Continuity across cycles',
        description:
          'Programs must survive leadership change without rewriting operational foundations.',
      },
      {
        id: 'services',
        title: 'Citizen service coordination',
        description:
          'Service pathways fragment when departments lack shared ownership of handoffs.',
      },
      {
        id: 'oversight',
        title: 'Oversight without paralysis',
        description:
          'Controls must support accountability without freezing day-to-day delivery.',
      },
      {
        id: 'reporting',
        title: 'Reporting integrity',
        description:
          'Leadership and auditors need evidence trails operators can still trust.',
      },
      {
        id: 'legacy',
        title: 'Legacy fragmentation',
        description:
          'Disconnected tools multiply risk precisely where coherence is most needed.',
      },
    ],
  },
  workflow: {
    title: 'Policy → Departments → Service delivery → Oversight → Reporting',
    description:
      'A public institution operating rhythm — transparent and durable.',
    steps: [
      'Policy',
      'Departments',
      'Service delivery',
      'Oversight',
      'Reporting',
    ],
  },
  portfolio: {
    title: 'How the AOVIAS portfolio supports government',
    description:
      'Shared architecture adapted to civic accountability — not a separate government product line.',
    items: [
      {
        id: 'command',
        name: 'AOVIAS Command',
        role: 'Operational control for program delivery — clear ownership across departments and partners.',
        href: '/products/command',
      },
      {
        id: 'studio',
        name: 'AOVIAS Studio',
        role: 'Publish policy-aligned standards and service designs that remain coherent over time.',
        href: '/products/studio',
      },
      {
        id: 'platform',
        name: 'AOVIAS Platform',
        role: 'Identity, security, and governance foundations built for institutional longevity.',
        href: '/products/platform',
      },
    ],
  },
  journey: {
    title: 'Agency → Policy → Departments → Services → Oversight',
    description:
      'The enterprise story for public programs that must remain explainable.',
    steps: [
      'Agency',
      'Policy',
      'Departments',
      'Service delivery',
      'Oversight',
      'Reporting',
    ],
  },
  narratives: {
    title: 'Grounded scenario narratives',
    description:
      'Believable institutional outcomes — not fabricated case studies.',
    items: [
      {
        id: 'program',
        title: 'Cross-department service coordination',
        description:
          'A public agency coordinates service delivery across departments with clearer ownership and oversight — without rebuilding systems for every program.',
      },
      {
        id: 'audit',
        title: 'Evidence-friendly operations',
        description:
          'Operators keep day-to-day control while leadership gains reporting integrity suitable for scrutiny and long program lifecycles.',
      },
    ],
  },
  closing: {
    title: 'Build public capability that can be explained.',
    description:
      'Request a demonstration for your agency, or speak with our team about institutional requirements.',
    primaryCta: {
      label: 'Request a demo',
      href: '/company/contact#form-demo',
    },
    secondaryCta: {
      label: 'Talk to our team',
      href: '/company/contact',
    },
    tertiaryCta: {
      label: 'Explore products',
      href: '/products',
    },
  },
} as const satisfies IndustryEditorialContent;
