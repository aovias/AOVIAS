import type { CareersModuleMeta } from '@/features/careers/types';

export const openRolesMeta = {
  id: 'open-roles',
  sectionId: 'open-roles',
  title: 'Open roles',
} as const satisfies CareersModuleMeta;

export const openRolesContent = {
  eyebrow: 'Open roles',
  heading: 'Roles we are hiring for',
  description:
    'Apply when the work and the mission both fit. We review applications carefully and respond thoughtfully.',
  emptyTitle: 'No open roles right now',
  emptyDescription:
    'We hire carefully and periodically. You may still introduce yourself — we read thoughtful notes.',
  generalApplyHref:
    'mailto:careers@aovias.com?subject=General%20introduction%20%E2%80%94%20AOVIAS',
  generalApplyLabel: 'Send a general introduction',
} as const;
