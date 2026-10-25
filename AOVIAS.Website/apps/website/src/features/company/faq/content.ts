import type { FaqItem } from '@/components/trust';
import type { CompanyModuleMeta } from '@/features/company/types';

export const faqMeta = {
  id: 'faq',
  sectionId: 'company-faq',
  title: 'Company FAQ',
} as const satisfies CompanyModuleMeta;

export const faqContent = {
  eyebrow: 'FAQ',
  heading: 'Company questions',
  description:
    'Compact answers about who AOVIAS is, what we build, and how institutions engage with us.',
} as const;

export const companyFaqItems: readonly FaqItem[] = [
  {
    id: 'who',
    question: 'Who is AOVIAS?',
    answer:
      'AOVIAS builds institutional software — platforms designed to strengthen operations, delivery discipline, and accountable judgment in organizations society depends on.',
    category: 'Company',
    keywords: ['about', 'company', 'institution'],
  },
  {
    id: 'products',
    question: 'What products does AOVIAS offer?',
    answer:
      'AOVIAS Command supports operational visibility and control. AOVIAS Studio is the delivery workspace for designing, refining, and shipping institutional work. Both are commercially available today.',
    category: 'Company',
    keywords: ['command', 'studio', 'products'],
  },
  {
    id: 'location',
    question: 'Where is AOVIAS based?',
    answer:
      'AOVIAS is registered in Khargone, Madhya Pradesh, India, and engages institutions globally through product, enterprise, and contact channels.',
    category: 'Company',
    keywords: ['khargone', 'madhya pradesh', 'india', 'location', 'office'],
  },
  {
    id: 'engage',
    question: 'How should institutions engage with AOVIAS?',
    answer:
      'Request a demo for product evaluation, Contact enterprise for licensing and procurement, or Talk with AOVIAS for a general inquiry. Paths are on the Contact page.',
    category: 'Engagement',
    keywords: ['demo', 'enterprise', 'contact'],
  },
  {
    id: 'careers',
    question: 'Does AOVIAS hire?',
    answer:
      'Yes. We hire carefully for engineering and related roles. Open roles and hiring philosophy are listed on the Careers page.',
    category: 'Careers',
    keywords: ['jobs', 'careers', 'hiring'],
  },
] as const;
