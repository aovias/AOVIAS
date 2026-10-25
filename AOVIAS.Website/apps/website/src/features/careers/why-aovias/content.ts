import type { CareersModuleMeta } from '@/features/careers/types';

export const whyMeta = {
  id: 'why-aovias',
  sectionId: 'why-aovias',
  title: 'Why AOVIAS',
} as const satisfies CareersModuleMeta;

export const whyContent = {
  eyebrow: 'Why AOVIAS',
  heading: 'Why people choose to build here',
  description:
    'Careers at AOVIAS are invitations to do careful, consequential engineering — work that strengthens institutions society depends on.',
  reasons: [
    {
      id: 'impact',
      heading: 'Institutional impact',
      description:
        'Your work supports organizations that educate, govern, heal, and operate at scale — where quality is felt by real people.',
    },
    {
      id: 'enduring-systems',
      heading: 'Enduring systems',
      description:
        'We build for longevity: clear architecture, measured change, and products that remain trustworthy under operational pressure.',
    },
    {
      id: 'growth',
      heading: 'Growth with substance',
      description:
        'Growth comes from ownership, rigorous feedback, and hard domain problems — not from title inflation or culture theater.',
    },
    {
      id: 'quality',
      heading: 'Engineering discipline',
      description:
        'We protect time for craftsmanship: reviews that matter, standards that hold, and decisions that can be explained years later.',
    },
  ],
} as const;
