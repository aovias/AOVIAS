import type { IndustryEditorialContent } from '@/features/industries/editorial/types';

/** Manufacturing — industrial, disciplined, resilient institutional narrative. */
export const manufacturingEditorialContent = {
  slug: 'manufacturing',
  name: 'Manufacturing',
  atmosphere: 'architecture',
  domainVariant: 'manufacturing',
  environmentVariant: 'deployment',
  seo: {
    title: 'Manufacturing — Production Continuity Platform',
    description:
      'AOVIAS for manufacturing: production continuity, quality systems, and asset reliability on a shared enterprise platform.',
    keywords: [
      'AOVIAS manufacturing',
      'production operations platform',
      'manufacturing quality systems',
      'plant operational visibility',
    ],
  },
  hero: {
    eyebrow: 'Manufacturing',
    title: 'Production continuity with disciplined control.',
    description:
      'Manufacturers need operational visibility across shifts and sites — quality, maintenance, and planning that stay coherent when exceptions rise.',
    primaryCta: {
      label: 'Request a demo',
      href: '/company/contact#form-demo',
    },
    secondaryCta: {
      label: 'Talk to our team',
      href: '/company/contact',
    },
    illustrationCaption: 'Production · quality · asset reliability',
  },
  reality: {
    title: 'The manufacturing operating environment',
    description:
      'Plants run on precision and resilience. Production continuity, quality systems, asset reliability, and efficiency are inseparable from institutional performance.',
    pillars: [
      'Production continuity',
      'Quality systems',
      'Asset reliability',
      'Operational efficiency',
    ],
  },
  challenges: {
    title: 'Operational challenges in manufacturing',
    description:
      'Pressures that arise across planning, production, quality, and maintenance.',
    items: [
      {
        id: 'continuity',
        title: 'Shift and site continuity',
        description:
          'Ownership of exceptions blurs across shifts, lines, and facilities.',
      },
      {
        id: 'quality',
        title: 'Quality system coherence',
        description:
          'Standards drift when quality work is disconnected from operational execution.',
      },
      {
        id: 'assets',
        title: 'Asset coordination',
        description:
          'Maintenance and production compete for attention without a shared operational picture.',
      },
      {
        id: 'visibility',
        title: 'Operational visibility',
        description:
          'Leaders need reliable signal on throughput and risk — not dashboard theater.',
      },
      {
        id: 'exceptions',
        title: 'Exception load',
        description:
          'Brittle workflows fail precisely when production pressure peaks.',
      },
      {
        id: 'integration',
        title: 'Plant system sprawl',
        description:
          'Disconnected plant and enterprise systems multiply reconciliation work.',
      },
    ],
  },
  workflow: {
    title: 'Planning → Production → Quality → Maintenance → Improvement',
    description:
      'A manufacturing operating rhythm — precise and resilient.',
    steps: [
      'Planning',
      'Production',
      'Quality',
      'Maintenance',
      'Continuous improvement',
    ],
  },
  portfolio: {
    title: 'How the AOVIAS portfolio supports manufacturing',
    description:
      'Shared platform architecture applied to production realities — not a factory-only product.',
    items: [
      {
        id: 'command',
        name: 'AOVIAS Command',
        role: 'Operational control for production exceptions, ownership, and continuity across sites.',
        href: '/products/command',
      },
      {
        id: 'studio',
        name: 'AOVIAS Studio',
        role: 'Standardize and publish quality and operating procedures that travel with the work.',
        href: '/products/studio',
      },
      {
        id: 'platform',
        name: 'AOVIAS Platform',
        role: 'Identity, deployment, and governed connectivity beneath plant and enterprise systems.',
        href: '/products/platform',
      },
    ],
  },
  journey: {
    title: 'Plant → Planning → Production → Quality → Leadership',
    description:
      'The enterprise story for manufacturing institutions under continuous operational pressure.',
    steps: [
      'Plant',
      'Planning',
      'Production',
      'Quality',
      'Maintenance',
      'Continuous improvement',
    ],
  },
  narratives: {
    title: 'Grounded scenario narratives',
    description:
      'Believable institutional outcomes — not fabricated case studies.',
    items: [
      {
        id: 'multisite',
        title: 'Multi-site production coherence',
        description:
          'A manufacturer aligns exception handling and quality standards across plants, improving continuity without fragmenting into site-specific tooling.',
      },
      {
        id: 'quality',
        title: 'Quality without bureaucracy',
        description:
          'Quality and production teams share an operational rhythm so improvement stays grounded in execution rather than disconnected reporting.',
      },
    ],
  },
  closing: {
    title: 'Strengthen production operations on one platform.',
    description:
      'Request a demonstration for your manufacturing environment, or speak with our team.',
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
