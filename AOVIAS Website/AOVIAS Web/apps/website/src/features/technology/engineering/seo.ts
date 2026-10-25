import type { BreadcrumbItem } from '@/lib/navigation/breadcrumb-types';

const base: BreadcrumbItem[] = [
  { label: 'Home', href: '/' },
  { label: 'Technology', href: '/technology' },
];

export const engineeringPagesSeo = {
  hub: {
    title: 'Engineering Philosophy',
    description:
      'How AOVIAS engineers enterprise software with discipline, quality by design, reliability, and long-term stewardship for institutions.',
    path: '/technology/engineering',
    keywords: [
      'enterprise software engineering',
      'engineering principles',
      'platform engineering',
      'AOVIAS',
      'reliable software',
    ],
  },
  architecture: {
    title: 'Engineering Architecture',
    description:
      'Platform-first, modular architecture philosophy: shared services, separation of concerns, extensibility, and future evolution — without implementation detail.',
    path: '/technology/engineering/architecture',
    keywords: [
      'software architecture',
      'platform architecture',
      'enterprise architecture',
      'AOVIAS',
    ],
  },
  designPrinciples: {
    title: 'Design Principles',
    description:
      'Engineering and product design principles: simplicity, consistency, composability, accessibility, security by design, performance, maintainability, and human-centered design.',
    path: '/technology/engineering/design-principles',
    keywords: [
      'engineering principles',
      'design principles',
      'enterprise software',
      'AOVIAS',
    ],
  },
  reliability: {
    title: 'Reliability',
    description:
      'Reliability as an engineering feature: availability intent, fault tolerance, predictable behavior, resilience, and continuous monitoring — without published SLAs.',
    path: '/technology/engineering/reliability',
    keywords: [
      'reliable software',
      'software reliability',
      'enterprise software',
      'AOVIAS',
    ],
  },
  scalability: {
    title: 'Scalability',
    description:
      'How AOVIAS designs for organizational growth, platform evolution, modular expansion, and multi-product strategy through sustainable architecture.',
    path: '/technology/engineering/scalability',
    keywords: [
      'scalable systems',
      'platform scalability',
      'enterprise software',
      'AOVIAS',
    ],
  },
  observability: {
    title: 'Observability',
    description:
      'Observability as operational excellence: monitoring, diagnostics, logging, metrics, health signals, and continuous feedback for reliable operations.',
    path: '/technology/engineering/observability',
    keywords: [
      'observability',
      'operational excellence',
      'platform engineering',
      'AOVIAS',
    ],
  },
} as const;

export function engineeringHubBreadcrumbs(): BreadcrumbItem[] {
  return [
    ...base,
    { label: 'Engineering', href: '/technology/engineering', current: true },
  ];
}

export function engineeringArchitectureBreadcrumbs(): BreadcrumbItem[] {
  return [
    ...base,
    { label: 'Engineering', href: '/technology/engineering' },
    {
      label: 'Architecture',
      href: '/technology/engineering/architecture',
      current: true,
    },
  ];
}

export function engineeringDesignPrinciplesBreadcrumbs(): BreadcrumbItem[] {
  return [
    ...base,
    { label: 'Engineering', href: '/technology/engineering' },
    {
      label: 'Design Principles',
      href: '/technology/engineering/design-principles',
      current: true,
    },
  ];
}

export function engineeringReliabilityBreadcrumbs(): BreadcrumbItem[] {
  return [
    ...base,
    { label: 'Engineering', href: '/technology/engineering' },
    {
      label: 'Reliability',
      href: '/technology/engineering/reliability',
      current: true,
    },
  ];
}

export function engineeringScalabilityBreadcrumbs(): BreadcrumbItem[] {
  return [
    ...base,
    { label: 'Engineering', href: '/technology/engineering' },
    {
      label: 'Scalability',
      href: '/technology/engineering/scalability',
      current: true,
    },
  ];
}

export function engineeringObservabilityBreadcrumbs(): BreadcrumbItem[] {
  return [
    ...base,
    { label: 'Engineering', href: '/technology/engineering' },
    {
      label: 'Observability',
      href: '/technology/engineering/observability',
      current: true,
    },
  ];
}

export const engineeringSitemapPaths = [
  '/technology/engineering',
  '/technology/engineering/architecture',
  '/technology/engineering/design-principles',
  '/technology/engineering/reliability',
  '/technology/engineering/scalability',
  '/technology/engineering/observability',
] as const;
