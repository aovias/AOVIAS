import type { CompanyModuleMeta } from '@/features/company/types';

export const philosophyMeta = {
  id: 'philosophy',
  sectionId: 'philosophy',
  title: 'Philosophy',
} as const satisfies CompanyModuleMeta;

export const philosophyContent = {
  eyebrow: 'Philosophy',
  heading: 'Principles that guide our work',
  description:
    'Product decisions are shaped by responsibility, human judgment, accountability, and steady improvement — not by trend pressure.',
  principles: [
    {
      id: 'responsible',
      principle: 'Responsible technology',
      description:
        'Capability is introduced with clear purpose, bounded use, and respect for institutional duty.',
    },
    {
      id: 'human-ai',
      principle: 'Human-centered intelligence',
      description:
        'AI and automation support people who remain accountable — they do not obscure ownership or judgment.',
    },
    {
      id: 'accountability',
      principle: 'Accountability by design',
      description:
        'Systems should make actions, access, and outcomes understandable to those who must answer for them.',
    },
    {
      id: 'improvement',
      principle: 'Continuous improvement',
      description:
        'Quality compounds through disciplined iteration, honest feedback, and long-horizon craftsmanship.',
    },
  ],
};
