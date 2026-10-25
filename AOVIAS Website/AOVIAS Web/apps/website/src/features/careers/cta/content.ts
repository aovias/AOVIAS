import type { CareersModuleMeta } from '@/features/careers/types';

export const ctaMeta = {
  id: 'careers-cta',
  sectionId: 'careers-cta',
  title: 'Next step',
} as const satisfies CareersModuleMeta;

export const ctaContent = {
  title: 'Ready to contribute?',
  description:
    'View open roles, send a focused application, or write to recruiting with a clear introduction. We respond thoughtfully.',
  primaryCta: {
    label: 'View open roles',
    href: '#open-roles',
  },
  secondaryCta: {
    label: 'Contact recruiting',
    href: 'mailto:careers@aovias.com?subject=Careers%20inquiry%20%E2%80%94%20AOVIAS',
  },
} as const;
