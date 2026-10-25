import type { ContactModuleMeta } from '@/features/contact/types';

export const heroMeta = {
  id: 'hero',
  sectionId: 'contact-hero',
  title: 'Contact',
} as const satisfies ContactModuleMeta;

export const heroContent = {
  eyebrow: 'Enterprise engagement',
  title: 'Begin a long-term technology partnership.',
  description:
    'Demo requests, enterprise inquiries, and sales conversations are consultation-led. Choose the path that matches your intent — we respond with clarity, not pressure.',
  primaryCta: {
    label: 'Request a demo',
    href: '#form-demo',
  },
  secondaryCta: {
    label: 'Contact enterprise',
    href: '#form-enterprise',
  },
  illustrationLabel: 'Partnership pathways for institutional engagement',
} as const;
