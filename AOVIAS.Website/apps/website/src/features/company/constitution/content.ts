import type { CompanyModuleMeta } from '@/features/company/types';

export const constitutionMeta = {
  id: 'constitution',
  sectionId: 'constitution',
  title: 'Constitution Summary',
} as const satisfies CompanyModuleMeta;

export const constitutionContent = {
  eyebrow: 'Constitution summary',
  heading: 'Values that shape how we build',
  description:
    'A public-friendly summary of the principles that govern AOVIAS work. This is not our internal governance document — it is the spirit of how we choose to operate.',
  principles: [
    {
      id: 'trust',
      principle: 'Trust is earned',
      description:
        'Credibility comes from delivery, restraint in claims, and systems that institutions can inspect and rely on.',
    },
    {
      id: 'discipline',
      principle: 'Engineering discipline',
      description:
        'Clarity, maintainability, and quality are non-negotiable — even when speed is tempting.',
    },
    {
      id: 'stewardship',
      principle: 'Stewardship of duty',
      description:
        'We treat institutional responsibility as a design constraint, not a marketing theme.',
    },
    {
      id: 'longevity',
      principle: 'Long-term commitment',
      description:
        'We optimize for partnerships and platforms that remain coherent across years of change.',
    },
  ],
};
