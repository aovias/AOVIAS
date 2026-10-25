import type { ContentAuthor } from '@/components/content';

import type { CompanyModuleMeta } from '@/features/company/types';

export const leadershipMeta = {
  id: 'leadership',
  sectionId: 'leadership',
  title: 'Leadership',
} as const satisfies CompanyModuleMeta;

export const leadershipContent = {
  eyebrow: 'Leadership',
  heading: 'Leadership as stewardship',
  description:
    'Leadership at AOVIAS means owning outcomes, protecting standards, and building teams that can carry institutional responsibility with care.',
  philosophy:
    'We design leadership for accountability: clear ownership of product and engineering quality, restraint in public claims, and room for executive and advisory roles as the institution grows — without inventing a roster we have not yet published.',
  principles: [
    {
      id: 'stewardship',
      title: 'Stewardship',
      description:
        'Protect standards and institutional purpose ahead of short-term optics.',
    },
    {
      id: 'accountability',
      title: 'Accountability',
      description:
        'Own outcomes clearly — including product quality, claims, and consequences.',
    },
    {
      id: 'long-term',
      title: 'Long-term Thinking',
      description:
        'Prefer durable architecture and partnerships over launch-window theatrics.',
    },
    {
      id: 'institutional',
      title: 'Institutional Responsibility',
      description:
        'Lead with duty to the organizations and people who depend on our systems.',
    },
  ],
  /** Public profile without inventing a personal name — expand when a verified roster is approved. */
  profiles: [
    {
      id: 'founder',
      name: 'AOVIAS Founding Leadership',
      role: 'Founder & Stewardship',
      organization: 'AOVIAS',
      avatarInitials: 'AV',
      biography:
        'Founding leadership is oriented to engineering discipline, institutional purpose, and long-term product stewardship. Named executive and advisory profiles will be published when they are ready for public presentation.',
    },
  ] satisfies ContentAuthor[],
  /**
   * Optional executive/advisory groups — empty until a verified roster is published.
   * Do not show dashed “coming soon” chrome on the public page.
   */
  groups: [] as ReadonlyArray<{
    id: string;
    title: string;
    description: string;
    emptyLabel?: string;
  }>,
};
