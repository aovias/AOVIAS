import type { CareersModuleMeta } from '@/features/careers/types';

export const cultureMeta = {
  id: 'culture',
  sectionId: 'culture',
  title: 'Culture',
} as const satisfies CareersModuleMeta;

export const cultureContent = {
  eyebrow: 'Engineering culture',
  heading: 'How we work together',
  description:
    'Our culture is practical: clear ownership, careful collaboration, and a long view of the systems we leave behind. Learn more about who we are on the Company page.',
  companyCultureHref: '/company#culture',
  principles: [
    {
      id: 'engineering-excellence',
      title: 'Engineering excellence',
      description:
        'We prefer precise interfaces, honest quality bars, and designs that remain understandable years later.',
    },
    {
      id: 'ownership',
      title: 'Ownership',
      description:
        'People own outcomes end to end — including trade-offs, follow-through, and the quiet work of maintenance.',
    },
    {
      id: 'continuous-learning',
      title: 'Continuous learning',
      description:
        'We improve through evidence, review, and disciplined reflection — not through slogans about hustle.',
    },
    {
      id: 'responsibility',
      title: 'Responsibility',
      description:
        'Institutional systems carry duty. We design with the people who depend on those systems in mind.',
    },
    {
      id: 'collaboration',
      title: 'Collaboration',
      description:
        'Shared standards and respectful critique make complex work safer and more coherent.',
    },
    {
      id: 'long-term-thinking',
      title: 'Long-term thinking',
      description:
        'We optimize for enduring systems and trusted relationships — not short cycles of attention.',
    },
  ],
} as const;
