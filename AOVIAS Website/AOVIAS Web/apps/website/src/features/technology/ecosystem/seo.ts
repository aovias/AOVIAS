import type { BreadcrumbItem } from '@/lib/navigation/breadcrumb-types';

const base: BreadcrumbItem[] = [
  { label: 'Home', href: '/' },
  { label: 'Technology', href: '/technology' },
];

export const ecosystemPagesSeo = {
  integrations: {
    title: 'Integrations',
    description:
      'How AOVIAS integrates with enterprise ecosystems — identity, healthcare, government, ERP, CRM, communications, and analytics — with interoperability by design.',
    path: '/technology/integrations',
    keywords: [
      'enterprise integrations',
      'enterprise interoperability',
      'platform integration',
      'AOVIAS',
    ],
  },
  apis: {
    title: 'Platform APIs (Future)',
    description:
      'AOVIAS API-first philosophy: secure, extensible platform services and future automation — without endpoint documentation. Developer resources forthcoming.',
    path: '/technology/integrations/apis',
    keywords: [
      'API-first platform',
      'platform APIs',
      'enterprise APIs',
      'AOVIAS',
    ],
  },
  developer: {
    title: 'Developer Platform (Future)',
    description:
      'Strategic vision for the AOVIAS Developer Platform: SDK philosophy, extensibility, automation, and developer experience — direction, not a current product catalogue.',
    path: '/technology/developer',
    keywords: [
      'developer platform',
      'enterprise developer experience',
      'platform extensibility',
      'AOVIAS',
    ],
  },
  roadmap: {
    title: 'Technology Roadmap',
    description:
      'High-level AOVIAS technology evolution: foundation, platform services, AI, integrations, developer platform, automation, and continuous innovation — without dates or release commitments.',
    path: '/technology/roadmap',
    keywords: [
      'technology roadmap',
      'enterprise platform roadmap',
      'platform evolution',
      'AOVIAS',
    ],
  },
  why: {
    title: 'Why AOVIAS Technology',
    description:
      'Why institutions choose AOVIAS: platform-first architecture, responsible AI, engineering discipline, security and trust, open integration, and long-term technology vision.',
    path: '/technology/why',
    keywords: [
      'AOVIAS technology',
      'enterprise technology partner',
      'platform-first',
      'responsible technology',
    ],
  },
} as const;

export function integrationsBreadcrumbs(): BreadcrumbItem[] {
  return [
    ...base,
    { label: 'Integrations', href: '/technology/integrations', current: true },
  ];
}

export function apisBreadcrumbs(): BreadcrumbItem[] {
  return [
    ...base,
    { label: 'Integrations', href: '/technology/integrations' },
    {
      label: 'Platform APIs',
      href: '/technology/integrations/apis',
      current: true,
    },
  ];
}

export function developerBreadcrumbs(): BreadcrumbItem[] {
  return [
    ...base,
    { label: 'Developer Platform', href: '/technology/developer', current: true },
  ];
}

export function roadmapBreadcrumbs(): BreadcrumbItem[] {
  return [
    ...base,
    { label: 'Technology Roadmap', href: '/technology/roadmap', current: true },
  ];
}

export function whyTechnologyBreadcrumbs(): BreadcrumbItem[] {
  return [
    ...base,
    { label: 'Why AOVIAS Technology', href: '/technology/why', current: true },
  ];
}

export const ecosystemSitemapPaths = [
  '/technology/integrations',
  '/technology/integrations/apis',
  '/technology/developer',
  '/technology/roadmap',
  '/technology/why',
] as const;
