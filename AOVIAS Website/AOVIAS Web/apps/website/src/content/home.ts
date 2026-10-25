import {
  BarChart3,
  Lock,
  ShieldCheck,
  Users,
} from 'lucide-react';

import type {
  MarketingCta,
  MarketingHighlight,
} from '@/components/marketing/types';

/**
 * Homepage content — V3.1 editorial refinement.
 * One idea per section. Depth lives on destination pages.
 */

export const homeSeo = {
  title: 'AOVIAS — Strengthening Institutions Through Technology',
  absoluteTitle: true as const,
  description:
    'AOVIAS builds dependable software for institutions society relies on — Command for operations, Studio for delivery, and a long-term platform commitment.',
  path: '/',
  keywords: [
    'AOVIAS',
    'enterprise software',
    'institutional technology',
    'responsible AI',
    'AOVIAS Command',
    'AOVIAS Studio',
  ],
} as const;

export const homeHero = {
  eyebrow: 'Institutional technology',
  title: 'Strengthening institutions through intelligent technology',
  description:
    'AOVIAS builds dependable software for organizations society relies on — with clarity, responsible use of intelligence, and a long-term view.',
  primaryCta: {
    label: 'Request a demo',
    href: '/company/contact#form-demo',
  } satisfies MarketingCta,
  secondaryCta: {
    label: 'Explore products',
    href: '/products',
  } satisfies MarketingCta,
  highlights: [
    {
      id: 'trusted',
      label: 'Trusted by institutions',
      icon: ShieldCheck,
    },
    {
      id: 'secure',
      label: 'Secure by design',
      icon: Lock,
    },
    {
      id: 'scale',
      label: 'Built for scale',
      icon: BarChart3,
    },
    {
      id: 'responsible-ai',
      label: 'Responsible AI',
      icon: Users,
    },
  ] as const satisfies readonly MarketingHighlight[],
};

/** Section 2 — Why AOVIAS exists (merged mission / vision / stewardship). */
export const homeWhy = {
  eyebrow: 'Institution',
  heading: 'Why AOVIAS exists',
  paragraphs: [
    'Institutions carry public trust. Technology that supports them must be steady, transparent, and worthy of that responsibility.',
    'We strengthen healthcare systems, public agencies, manufacturers, and financial institutions with intelligent tools that improve capability without compromising integrity.',
  ],
  cta: {
    label: 'About AOVIAS',
    href: '/company',
  } satisfies MarketingCta,
};

/** Section 3–4 — Ecosystem intro + product cards (one portfolio idea). */
export const homeEcosystem = {
  eyebrow: 'Platform',
  heading: 'One platform. A focused portfolio.',
  description:
    'Command, Studio, and the AOVIAS Platform share a foundation — so institutions grow capability without fragmenting their stack.',
};

export const homeProducts = {
  items: [
    {
      id: 'command' as const,
      title: 'AOVIAS Command',
      description:
        'Operations workspace for visibility, structured control, and continuity.',
      href: '/products/command',
      linkLabel: 'Explore Command',
      illustrationId: 'ill-command-terminal' as const,
    },
    {
      id: 'studio' as const,
      title: 'AOVIAS Studio',
      description:
        'Delivery workspace to design, standardize, and ship institutional work.',
      href: '/products/studio',
      linkLabel: 'Explore Studio',
      illustrationId: 'ill-studio-workstation' as const,
    },
    {
      id: 'platform' as const,
      title: 'AOVIAS Platform',
      description:
        'Shared standards for security, identity, and interoperability.',
      href: '/products/platform',
      linkLabel: 'Explore Platform',
      illustrationId: 'ill-platform-core' as const,
    },
  ],
};

/** Section 5 — Technology pillars (cards only). */
export const homeTechnology = {
  eyebrow: 'Technology',
  heading: 'Built on four pillars',
  description: 'Go deeper where your questions live.',
  items: [
    {
      id: 'platform' as const,
      title: 'Platform',
      description: 'Shared services that keep every product coherent.',
      href: '/technology/platform',
      linkLabel: 'Explore Platform',
    },
    {
      id: 'ai' as const,
      title: 'AI',
      description: 'Assistive intelligence with human oversight.',
      href: '/technology/ai',
      linkLabel: 'Explore AI',
    },
    {
      id: 'engineering' as const,
      title: 'Engineering',
      description: 'Architecture, reliability, and operational discipline.',
      href: '/technology/engineering',
      linkLabel: 'Explore Engineering',
    },
    {
      id: 'security' as const,
      title: 'Trust',
      description: 'Security, identity, and stewardship as fundamentals.',
      href: '/technology/trust',
      linkLabel: 'Explore Trust',
    },
  ],
};

/** Section 6 — Single architecture preview. */
export const homeArchitecture = {
  eyebrow: 'Architecture',
  heading: 'Designed to be understood',
  description:
    'Clear layers and boundaries — so institutions can adopt capability without redesigning their foundation.',
  cta: {
    label: 'Explore Architecture',
    href: '/technology/engineering/architecture',
  } satisfies MarketingCta,
};

/** Section 7 — Industries. */
export const homeIndustries = {
  eyebrow: 'Industries',
  heading: 'Built for institutions that matter',
  description:
    'One platform, adapted to sectors where accuracy and continuity are non-negotiable.',
  items: [
    {
      id: 'healthcare',
      title: 'Healthcare',
      description: 'Care, compliance, and operational continuity.',
      href: '/solutions/healthcare',
      linkLabel: 'Explore',
    },
    {
      id: 'government',
      title: 'Government',
      description: 'Transparent systems for civic responsibility.',
      href: '/solutions/government',
      linkLabel: 'Explore',
    },
    {
      id: 'manufacturing',
      title: 'Manufacturing',
      description: 'Durable operations across complex environments.',
      href: '/solutions/manufacturing',
      linkLabel: 'Explore',
    },
    {
      id: 'financial-services',
      title: 'Financial Services',
      description: 'Governance, control, and long-term reliability.',
      href: '/solutions/financial-services',
      linkLabel: 'Explore',
    },
  ],
};

/** Section 8 — Trust (one visual + three cards). */
export const homeTrust = {
  eyebrow: 'Trust',
  heading: 'Trust you can see',
  description:
    'Security, governance, and reliability — expressed as a system, not slogans.',
  cards: [
    {
      id: 'security',
      title: 'Security',
      description: 'Protection and stewardship treated as product fundamentals.',
      href: '/technology/trust',
      linkLabel: 'Security & trust',
    },
    {
      id: 'governance',
      title: 'Governance',
      description: 'Clear ownership, oversight, and measured claims.',
      href: '/company',
      linkLabel: 'Our approach',
    },
    {
      id: 'reliability',
      title: 'Reliability',
      description: 'Continuity for institutions that cannot afford careless change.',
      href: '/technology/engineering/reliability',
      linkLabel: 'Engineering',
    },
  ],
};

/** Section 9 — Closing CTA. */
export const homeCta = {
  title: 'Ready to strengthen your institution?',
  description:
    'Speak with AOVIAS about your environment, or request a focused demonstration.',
  primaryCta: {
    label: 'Request a demo',
    href: '/company/contact#form-demo',
  } satisfies MarketingCta,
};
