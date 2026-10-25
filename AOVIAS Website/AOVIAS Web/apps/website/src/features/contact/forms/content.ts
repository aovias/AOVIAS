import { Building2, Handshake, MessagesSquare, MonitorPlay } from 'lucide-react';

import type { ContactModuleMeta } from '@/features/contact/types';

export const formsMeta = {
  id: 'forms',
  sectionId: 'contact-forms',
  title: 'Engagement forms',
} as const satisfies ContactModuleMeta;

export const formsContent = {
  contact: {
    id: 'form-contact',
    eyebrow: 'Contact sales',
    heading: 'Talk with AOVIAS',
    cardDescription: 'Start a consultation with our institutional team.',
    description:
      'Share institutional context so we can route your conversation accurately.',
    icon: MessagesSquare,
  },
  demo: {
    id: 'form-demo',
    eyebrow: 'Demo request',
    heading: 'Request a Demonstration',
    cardDescription: 'Schedule a focused walkthrough of Command or Studio.',
    description:
      'Tell us about your organization, product interest, and stakeholders.',
    icon: MonitorPlay,
  },
  enterprise: {
    id: 'form-enterprise',
    eyebrow: 'Enterprise inquiry',
    heading: 'Contact Enterprise',
    cardDescription: 'Discuss procurement, security review, and rollout fit.',
    description:
      'Share organization details, industry context, and deployment requirements.',
    icon: Building2,
  },
  partner: {
    id: 'form-partner',
    eyebrow: 'Partnerships',
    heading: 'Partner Inquiry',
    cardDescription: 'Explore technology, reseller, or advisory partnerships.',
    description:
      'Describe your organization, partnership type, and business overview.',
    icon: Handshake,
  },
} as const;

export const engagementPanelContent = {
  title: 'Engagement information',
  phone: {
    label: 'Phone',
    value: '+91 91312 57667',
    href: 'tel:+919131257667',
  },
  email: {
    label: 'Email',
    value: 'admin@aovias.com',
    href: 'mailto:admin@aovias.com',
  },
  response: {
    label: 'Typical response',
    value: 'Within one business day',
  },
  hours: {
    label: 'Business hours',
    value: 'Monday–Friday\n09:30–18:30 IST',
  },
  nextSteps: {
    label: 'What happens next',
    steps: [
      'Submit enquiry',
      'Team review',
      'Discovery discussion',
      'Product demonstration',
      'Proposal / Next steps',
    ],
  },
  trust: {
    label: 'Why institutions engage AOVIAS',
    items: [
      'Enterprise Software',
      'Built in India',
      'Privacy by Design',
      'Long-term Product Support',
    ],
  },
} as const;
