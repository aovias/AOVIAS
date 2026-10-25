import { companyLegal } from '@/config/company-legal';

import type { ContactModuleMeta } from '@/features/contact/types';

export const officeMeta = {
  id: 'office',
  sectionId: 'office',
  title: 'Office information',
} as const satisfies ContactModuleMeta;

export const officeContent = {
  eyebrow: 'Presence',
  heading: 'Where and how to reach us',
  description:
    'AOVIAS is registered in Khargone, Madhya Pradesh, and engages institutions globally through the channels below.',
  headquarters: {
    title: 'Registered office',
    lines: [
      companyLegal.legalName,
      ...companyLegal.registeredOffice,
      'Serving institutional customers globally',
    ],
  },
  hours: {
    title: 'Business hours',
    description:
      'Monday–Friday, 09:30–18:30 IST. Messages received outside these hours are addressed on the next business day.',
  },
  channels: {
    title: 'Contact channels',
    items: [
      {
        id: 'phone',
        label: 'Phone',
        value: companyLegal.phoneDisplay,
        href: `tel:${companyLegal.phoneTel}`,
      },
      {
        id: 'email',
        label: 'Email',
        value: companyLegal.contactEmail,
        href: `mailto:${companyLegal.contactEmail}`,
      },
      {
        id: 'support',
        label: 'Support',
        value: companyLegal.supportEmail,
        href: `mailto:${companyLegal.supportEmail}`,
      },
      {
        id: 'web',
        label: 'Web forms',
        value: 'Engagement options on this page',
        href: '#contact-forms',
      },
    ],
  },
} as const;
