import type { CompanyModuleMeta } from '@/features/company/types';

export const missionMeta = {
  id: 'mission',
  sectionId: 'mission',
  title: 'Mission',
} as const satisfies CompanyModuleMeta;

export const missionContent = {
  eyebrow: 'Mission',
  heading: 'Strengthen institutions through intelligent technology',
  description:
    'Our mission centers on institutional impact — capability, integrity, and continuity — not commercial theater.',
  statement:
    'AOVIAS exists to help the organizations society depends on work with greater clarity, responsibility, and resilience.',
  outcomes: [
    {
      id: 'capability',
      heading: 'Greater capability',
      description:
        'Tools that help teams see clearly, decide carefully, and operate with consistency.',
    },
    {
      id: 'integrity',
      heading: 'Preserved integrity',
      description:
        'Systems designed for accountability, measured claims, and responsible use of intelligence.',
    },
    {
      id: 'continuity',
      heading: 'Operational continuity',
      description:
        'Technology that can be trusted across years of institutional duty, not just a launch window.',
    },
  ],
};
