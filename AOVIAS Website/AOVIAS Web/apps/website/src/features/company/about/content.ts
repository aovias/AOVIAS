import type { MarketingCta } from '@/components/marketing/types';

import type { CompanyModuleMeta } from '@/features/company/types';

export const aboutMeta = {
  id: 'about',
  sectionId: 'about',
  title: 'About AOVIAS',
} as const satisfies CompanyModuleMeta;

export const aboutContent = {
  eyebrow: 'Company',
  /** Two-line headline; accent period rendered in the section. */
  titleLines: ['Engineering Software', 'Built for Institutions'] as const,
  lead: 'AOVIAS builds dependable software for institutions that society depends on.',
  sectors: [
    'Healthcare',
    'Government',
    'Education',
    'Enterprise',
  ] as const,
  focus:
    'Engineered for accountability, clarity, and operational continuity.',
  primaryCta: {
    label: 'Explore Products',
    href: '/products',
  } satisfies MarketingCta,
  secondaryCta: {
    label: 'Read Our Mission',
    href: '#mission',
  } satisfies MarketingCta,
  trustSignals: [
    'Built in India',
    'Enterprise Software',
    'Privacy by Design',
    'Institution-first Engineering',
  ] as const,
} as const;
