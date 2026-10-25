import type { CareersModuleMeta } from '@/features/careers/types';

export const heroMeta = {
  id: 'hero',
  sectionId: 'careers-hero',
  title: 'Careers',
} as const satisfies CareersModuleMeta;

export const heroContent = {
  eyebrow: 'Careers',
  title: 'Build technology that institutions can rely on',
  description:
    'AOVIAS invites thoughtful engineers and builders who want their craft to strengthen institutions society depends on — purpose before titles, substance before spectacle.',
  primaryCta: {
    label: 'View open roles',
    href: '#open-roles',
  },
  secondaryCta: {
    label: 'Hiring philosophy',
    href: '#hiring-process',
  },
  illustrationLabel: 'Contribution over recruitment theatre',
} as const;
