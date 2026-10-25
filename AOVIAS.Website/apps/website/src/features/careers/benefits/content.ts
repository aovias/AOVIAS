import type { CareersModuleMeta } from '@/features/careers/types';

export const benefitsMeta = {
  id: 'benefits',
  sectionId: 'benefits',
  title: 'Benefits',
} as const satisfies CareersModuleMeta;

/**
 * Benefits principles — honest, principle-level working conditions.
 */
export const benefitsContent = {
  eyebrow: 'Working here',
  heading: 'Principles of working at AOVIAS',
  description:
    'These are the conditions we aim to uphold as we grow. Details vary by role and location; the principles stay consistent.',
  items: [
    {
      id: 'learning',
      title: 'Learning',
      description:
        'Time and expectation for deliberate learning — reading, reviews, and skill deepening tied to real delivery.',
    },
    {
      id: 'flexible-work',
      title: 'Flexible work',
      description:
        'Remote-friendly collaboration with clear communication norms, so focus and presence can coexist.',
    },
    {
      id: 'growth',
      title: 'Growth opportunities',
      description:
        'Paths that reward ownership and craft — broader scope through trust earned, not through theatre.',
    },
    {
      id: 'modern-practices',
      title: 'Modern engineering practices',
      description:
        'CI, thoughtful reviews, observability, and documentation treated as first-class product work.',
    },
    {
      id: 'sustainable-pace',
      title: 'Sustainable pace',
      description:
        'We prefer steady excellence over burnout cycles. Urgency is reserved for genuine institutional need.',
    },
    {
      id: 'health-support',
      title: 'Health & wellbeing support',
      description:
        'Practical support for health and recovery so people can do durable work over years, not weeks.',
    },
  ],
} as const;
