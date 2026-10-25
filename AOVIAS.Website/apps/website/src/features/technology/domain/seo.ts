import type { BreadcrumbItem } from '@/lib/navigation/breadcrumb-types';

import { getTechnologyTopicBySlug } from './registry';

export const technologyLandingSeo = {
  title: 'Technology',
  description:
    'Engineering philosophy, platform architecture, and technical capabilities behind AOVIAS — built for institutions that require clarity, trust, and scale.',
  path: '/technology',
  keywords: [
    'AOVIAS technology',
    'enterprise architecture',
    'platform engineering',
    'responsible AI',
    'institutional software',
  ],
} as const;

export function technologyTopicSeo(slug: string) {
  const topic = getTechnologyTopicBySlug(slug);
  const label = topic?.label ?? 'Technology';
  const description =
    topic?.description ??
    'AOVIAS technology topic — detailed content will be published in a later release.';

  return {
    title: `${label} | Technology`,
    description,
    path: `/technology/${slug}`,
    keywords: ['AOVIAS technology', label],
  } as const;
}

export function technologyLandingBreadcrumbs(): BreadcrumbItem[] {
  return [
    { label: 'Home', href: '/' },
    { label: 'Technology', href: '/technology', current: true },
  ];
}

export function technologyTopicBreadcrumbs(slug: string): BreadcrumbItem[] {
  const topic = getTechnologyTopicBySlug(slug);
  return [
    { label: 'Home', href: '/' },
    { label: 'Technology', href: '/technology' },
    {
      label: topic?.label ?? 'Topic',
      href: `/technology/${slug}`,
      current: true,
    },
  ];
}
