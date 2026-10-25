import type { ContactModuleMeta } from '@/features/contact/types';

export const optionsMeta = {
  id: 'engagement-options',
  sectionId: 'engagement-options',
  title: 'Engagement options',
} as const satisfies ContactModuleMeta;

export const optionsContent = {
  eyebrow: 'Enterprise inquiry',
  heading: 'Specialized contact paths by intent',
  description:
    'Route your conversation to the right team — enterprise sales, platform evaluation, industry solutions, partnerships, or technical questions.',
} as const;
