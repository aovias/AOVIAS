import type { BreadcrumbItem } from '@/lib/navigation/breadcrumb-types';

const homeTech: BreadcrumbItem[] = [
  { label: 'Home', href: '/' },
  { label: 'Technology', href: '/technology' },
];

export const platformPagesSeo = {
  hub: {
    title: 'AOVIAS Platform',
    description:
      'One enterprise platform powering AOVIAS products — shared services, engineering standards, security by design, and responsible intelligence as a capability.',
    path: '/technology/platform',
    keywords: [
      'AOVIAS platform',
      'enterprise platform',
      'platform architecture',
      'shared services',
    ],
  },
  overview: {
    title: 'Platform Overview',
    description:
      'Platform-first philosophy: shared capabilities, products on one foundation, operational consistency, and enterprise extensibility.',
    path: '/technology/platform/overview',
    keywords: ['platform overview', 'AOVIAS platform', 'platform-first'],
  },
  services: {
    title: 'Platform Services',
    description:
      'Conceptual shared services — identity, runtime, publishing, deployment, AI orchestration, notifications, search, analytics, and integrations.',
    path: '/technology/platform/services',
    keywords: ['platform services', 'shared services', 'AOVIAS'],
  },
  ecosystem: {
    title: 'Platform Ecosystem',
    description:
      'How the AOVIAS Platform connects shared services, products, organizations, people, and societal outcomes.',
    path: '/technology/platform/ecosystem',
    keywords: ['platform ecosystem', 'AOVIAS', 'institutional technology'],
  },
  architecture: {
    title: 'Platform Architecture Overview',
    description:
      'Architectural philosophy: modular, service-oriented, registry-driven, secure-by-design, AI-ready, scalable, reliable, and observable.',
    path: '/technology/architecture',
    keywords: [
      'platform architecture',
      'enterprise architecture',
      'AOVIAS',
      'secure by design',
    ],
  },
} as const;

export function platformHubBreadcrumbs(): BreadcrumbItem[] {
  return [
    ...homeTech,
    { label: 'Platform', href: '/technology/platform', current: true },
  ];
}

export function platformOverviewBreadcrumbs(): BreadcrumbItem[] {
  return [
    ...homeTech,
    { label: 'Platform', href: '/technology/platform' },
    { label: 'Overview', href: '/technology/platform/overview', current: true },
  ];
}

export function platformServicesBreadcrumbs(): BreadcrumbItem[] {
  return [
    ...homeTech,
    { label: 'Platform', href: '/technology/platform' },
    { label: 'Services', href: '/technology/platform/services', current: true },
  ];
}

export function platformEcosystemBreadcrumbs(): BreadcrumbItem[] {
  return [
    ...homeTech,
    { label: 'Platform', href: '/technology/platform' },
    { label: 'Ecosystem', href: '/technology/platform/ecosystem', current: true },
  ];
}

export function platformArchitectureBreadcrumbs(): BreadcrumbItem[] {
  return [
    ...homeTech,
    {
      label: 'Architecture',
      href: '/technology/architecture',
      current: true,
    },
  ];
}

export const platformSitemapPaths = [
  '/technology/platform',
  '/technology/platform/overview',
  '/technology/platform/services',
  '/technology/platform/ecosystem',
  '/technology/architecture',
] as const;
