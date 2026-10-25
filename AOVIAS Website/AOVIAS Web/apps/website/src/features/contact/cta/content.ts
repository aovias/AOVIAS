import type { ContactModuleMeta } from '@/features/contact/types';

export const ctaMeta = {
  id: 'contact-cta',
  sectionId: 'contact-cta',
  title: 'Next step',
} as const satisfies ContactModuleMeta;

export const ctaContent = {
  title: 'Not sure which path to choose?',
  description:
    'Start with Enterprise Sales or a Platform Evaluation demo. We route inquiries carefully — partnership first, without pressure.',
  primaryCta: {
    label: 'Contact sales',
    href: '#form-contact',
  },
  secondaryCta: {
    label: 'Request a demo',
    href: '#form-demo',
  },
} as const;
