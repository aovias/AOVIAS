import type { IndustryEditorialContent } from '@/features/industries/editorial/types';

/** Financial Services — measured, trusted, analytical institutional narrative. */
export const financialServicesEditorialContent = {
  slug: 'financial-services',
  name: 'Financial Services',
  atmosphere: 'trust-geometry',
  domainVariant: 'financial',
  environmentVariant: 'trust',
  seo: {
    title: 'Financial Services — Governed Institutional Platform',
    description:
      'AOVIAS for financial services: trust, risk, governance, and operational control on a shared enterprise platform.',
    keywords: [
      'AOVIAS financial services',
      'financial operations platform',
      'risk governance software',
      'institutional control systems',
    ],
  },
  hero: {
    eyebrow: 'Financial Services',
    title: 'Trust under continuous oversight.',
    description:
      'Financial institutions need operational control that stands up to risk, audit, and governance — measured capability without speculative theater.',
    primaryCta: {
      label: 'Request a demo',
      href: '/company/contact#form-demo',
    },
    secondaryCta: {
      label: 'Talk to our team',
      href: '/company/contact',
    },
    illustrationCaption: 'Risk · governance · operational control',
  },
  reality: {
    title: 'The financial services operating environment',
    description:
      'Trust is foundational. Risk, governance, and operational control must remain coherent as products, partners, and regulations evolve.',
    pillars: ['Trust', 'Risk', 'Governance', 'Operational control'],
  },
  challenges: {
    title: 'Operational challenges in financial institutions',
    description:
      'Pressures that arise where fiduciary duty and complex operations meet.',
    items: [
      {
        id: 'risk',
        title: 'Risk control coherence',
        description:
          'Controls fragment when risk, operations, and product teams lack a shared operating picture.',
      },
      {
        id: 'audit',
        title: 'Auditability at speed',
        description:
          'Evidence trails must keep pace with change without freezing delivery.',
      },
      {
        id: 'governance',
        title: 'Governance that operators can use',
        description:
          'Policy that exists only in documents fails when exception load rises.',
      },
      {
        id: 'oversight',
        title: 'Institutional oversight',
        description:
          'Leadership needs attributable visibility — not vanity metrics.',
      },
      {
        id: 'change',
        title: 'Controlled change',
        description:
          'Release and process change carry real risk when ownership is unclear.',
      },
      {
        id: 'sprawl',
        title: 'Tool sprawl under regulation',
        description:
          'Disconnected systems multiply reconciliation precisely where trust is non-negotiable.',
      },
    ],
  },
  workflow: {
    title: 'Policy → Risk controls → Operations → Audit → Governance',
    description:
      'A financial institution operating rhythm — measured and attributable.',
    steps: [
      'Policy',
      'Risk controls',
      'Operations',
      'Audit',
      'Governance',
    ],
  },
  portfolio: {
    title: 'How the AOVIAS portfolio supports financial services',
    description:
      'One platform applied to trust-critical work — not a finance-only product silo.',
    items: [
      {
        id: 'command',
        name: 'AOVIAS Command',
        role: 'Operational control with clear ownership for risk-aware institutional work.',
        href: '/products/command',
      },
      {
        id: 'studio',
        name: 'AOVIAS Studio',
        role: 'Author and version policies, controls, and operating standards with review discipline.',
        href: '/products/studio',
      },
      {
        id: 'platform',
        name: 'AOVIAS Platform',
        role: 'Identity, security, and governance services that compound portfolio trust.',
        href: '/products/platform',
      },
    ],
  },
  journey: {
    title: 'Institution → Policy → Controls → Operations → Leadership',
    description:
      'The enterprise story for financial institutions that must remain trustworthy under oversight.',
    steps: [
      'Institution',
      'Policy',
      'Risk controls',
      'Operations',
      'Audit',
      'Governance',
    ],
  },
  narratives: {
    title: 'Grounded scenario narratives',
    description:
      'Believable institutional outcomes — not fabricated case studies.',
    items: [
      {
        id: 'controls',
        title: 'Risk controls that operators can execute',
        description:
          'A financial institution aligns policy, risk controls, and day-to-day operations so oversight improves without adding opaque process layers.',
      },
      {
        id: 'audit',
        title: 'Audit-ready operational rhythm',
        description:
          'Teams keep delivery moving while attributable records support audit and governance conversations with measured confidence.',
      },
    ],
  },
  closing: {
    title: 'Operate with confidence under oversight.',
    description:
      'Request a demonstration for your institution, or speak with our team about governed platform fit.',
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
