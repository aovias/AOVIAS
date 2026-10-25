import type { FaqItem } from '@/components/trust';
import type { ContactModuleMeta } from '@/features/contact/types';

export const faqMeta = {
  id: 'faq',
  sectionId: 'contact-faq',
  title: 'Contact FAQ',
} as const satisfies ContactModuleMeta;

export const faqContent = {
  eyebrow: 'FAQ',
  heading: 'Engagement questions',
  description:
    'Which path to use, what happens after a demo request, and how enterprise conversations proceed.',
} as const;

export const contactFaqItems: readonly FaqItem[] = [
  {
    id: 'which-form',
    question: 'Which path should I use?',
    answer:
      'Platform Evaluation for a demonstration. Enterprise Engagement for licensing, procurement, or multi-site deployments. Healthcare Solutions and Government Engagement for sector-specific conversations. Enterprise Sales for commercial fit. Partnerships and Technical Questions have their own paths.',
    category: 'Paths',
    keywords: ['form', 'demo', 'enterprise', 'contact', 'healthcare', 'government'],
  },
  {
    id: 'demo',
    question: 'What happens after I request a demo?',
    answer:
      'We confirm receipt, clarify product and stakeholders, schedule a short discovery conversation, then deliver a focused technical demonstration — not a generic marketing presentation. Typical scheduling outreach is within two business days.',
    category: 'Demo',
    keywords: ['demo', 'walkthrough', 'schedule', 'discovery'],
  },
  {
    id: 'enterprise',
    question: 'How do enterprise inquiries work?',
    answer:
      'Share organization details, industry context, size, and requirements. An enterprise specialist follows up with a practical next step — often covering licensing, security review, or deployment scope. The goal is partnership fit, not pressure.',
    category: 'Enterprise',
    keywords: ['enterprise', 'procurement', 'licensing'],
  },
  {
    id: 'response-time',
    question: 'How quickly do you respond?',
    answer:
      'Most inquiries receive a response within two business days during Monday–Friday, 09:30–18:30 IST. Messages outside those hours are addressed on the next business day.',
    category: 'Response',
    keywords: ['response', 'hours', 'SLA'],
  },
  {
    id: 'office',
    question: 'Where is AOVIAS located?',
    answer:
      'AOVIAS is registered in Khargone, Madhya Pradesh, India. For commercial conversations, use the forms on this page or email admin@aovias.com.',
    category: 'Presence',
    keywords: ['khargone', 'office', 'location'],
  },
] as const;
