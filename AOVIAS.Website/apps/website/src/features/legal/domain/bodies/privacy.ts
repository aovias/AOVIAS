import type { LegalBodySection } from '@/features/legal/types';

/**
 * Privacy Policy body — public draft for counsel review.
 * Not legal advice; not counsel-approved.
 */
export const privacyBody: readonly LegalBodySection[] = [
  {
    id: 'notice',
    heading: 'Public draft notice',
    paragraphs: [
      'This Privacy Policy is a public draft published for transparency and counsel review. It is not legal advice and is not counsel-approved. Product-specific or enterprise agreements may include additional terms.',
    ],
  },
  {
    id: 'introduction',
    heading: 'Introduction',
    paragraphs: [
      'AOVIAS protects personal information with clarity and care. This policy explains how we collect, use, and safeguard information when you visit the AOVIAS public website and related public services.',
      'We process personal information for stated purposes only. We do not sell personal information.',
    ],
  },
  {
    id: 'information-collection',
    heading: 'Information we collect',
    paragraphs: [
      'We collect information you voluntarily provide and limited technical information needed to operate the website securely and reliably.',
    ],
    bullets: [
      'Contact details you submit through forms — such as name, email, organization, and message content.',
      'Account-related details if you create or manage an account through Identity experiences.',
      'Billing profile details you enter during self-serve checkout, processed only as far as the checkout experience supports. Payment processing, when enabled, is handled by the payment provider.',
      'Technical data such as browser type, approximate region derived from network signals, and diagnostic logs needed for reliability and security.',
    ],
  },
  {
    id: 'usage',
    heading: 'How we use information',
    paragraphs: [
      'We use personal information to respond to inquiries, operate services you request, improve reliability, and meet legitimate security and compliance needs.',
    ],
    bullets: [
      'Respond to contact, careers, and enterprise engagement requests.',
      'Provide account, documentation, and product access experiences you initiate.',
      'Maintain security, prevent abuse, and diagnose service issues.',
      'Send service-related notices when appropriate.',
    ],
  },
  {
    id: 'sharing',
    heading: 'When we share information',
    paragraphs: [
      'We may share personal information with service providers who process data on our behalf under contractual obligations appropriate to the service — for example hosting, email delivery, or payment processing when enabled.',
      'We may disclose information when required by law, to protect rights and safety, or in connection with a corporate transaction, subject to applicable safeguards.',
    ],
  },
  {
    id: 'data-protection',
    heading: 'Data protection',
    paragraphs: [
      'We apply organizational and technical safeguards appropriate to the sensitivity of the information and the nature of the service.',
      'Access to personal information is limited to authorized personnel and processors who need it to perform their roles.',
    ],
  },
  {
    id: 'user-rights',
    heading: 'Your rights',
    paragraphs: [
      'Depending on your location and applicable law, you may have rights to access, correct, delete, or restrict processing of personal information, or to object to certain processing.',
      'To exercise a privacy request, contact us using the details below. We may need to verify your identity before fulfilling a request.',
    ],
  },
  {
    id: 'retention',
    heading: 'Retention',
    paragraphs: [
      'We retain personal information only as long as needed for the purposes described, including legal, security, and operational requirements. Retention periods vary by data category and context.',
    ],
  },
  {
    id: 'international',
    heading: 'International transfers',
    paragraphs: [
      'If personal information is processed in countries other than where you reside, we take steps intended to provide appropriate protection consistent with this policy and applicable law.',
    ],
  },
  {
    id: 'contact-information',
    heading: 'Contact',
    paragraphs: [
      'For privacy questions or requests, contact privacy@aovias.com or use the Contact page. We aim to respond promptly and clearly.',
    ],
  },
  {
    id: 'updates',
    heading: 'Updates to this policy',
    paragraphs: [
      'We may update this draft as practices, products, or legal requirements evolve. The version and last-updated date appear at the top of this page. Material changes will be reflected in a new version of this document.',
    ],
  },
];
