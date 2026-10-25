import type { TimelineEvent } from '@/components/marketing';
import type { CareersModuleMeta } from '@/features/careers/types';

export const hiringMeta = {
  id: 'hiring-process',
  sectionId: 'hiring-process',
  title: 'Hiring process',
} as const satisfies CareersModuleMeta;

export const hiringContent = {
  eyebrow: 'Hiring philosophy',
  heading: 'A clear, respectful hiring journey',
  description:
    'We hire carefully and periodically. Each stage exists to understand mutual fit — substance over spectacle, predictability over surprise.',
  stages: [
    {
      id: 'application',
      title: 'Application',
      description:
        'Share a concise note on why the role fits your experience, with links to work you are proud of.',
    },
    {
      id: 'conversation',
      title: 'Initial conversation',
      description:
        'A focused discussion about context, expectations, and whether the mission resonates.',
    },
    {
      id: 'assessment',
      title: 'Technical assessment',
      description:
        'Practical work aligned to the role — clarity and judgment matter more than clever tricks.',
    },
    {
      id: 'team-interview',
      title: 'Team interview',
      description:
        'Meet the people you would collaborate with; we value mutual questions and honest trade-offs.',
    },
    {
      id: 'final-discussion',
      title: 'Final discussion',
      description:
        'Align on scope, working norms, and remaining questions before any decision.',
    },
    {
      id: 'offer',
      title: 'Offer',
      description:
        'A clear written offer with room to discuss details thoughtfully.',
    },
  ] as const satisfies readonly TimelineEvent[],
};
