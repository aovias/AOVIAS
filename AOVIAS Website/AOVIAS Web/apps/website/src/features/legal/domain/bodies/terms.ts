import type { LegalBodySection } from '@/features/legal/types';

/**
 * Terms of Service body — public draft for counsel review.
 * Not legal advice; not counsel-approved.
 */
export const termsBody: readonly LegalBodySection[] = [
  {
    id: 'notice',
    heading: 'Public draft notice',
    paragraphs: [
      'These Terms of Service are a public draft published for transparency and counsel review. They are not legal advice and are not counsel-approved. Separate agreements may govern paid products, enterprise deployments, or partner arrangements.',
    ],
  },
  {
    id: 'introduction',
    heading: 'Introduction',
    paragraphs: [
      'These terms describe conditions for using the AOVIAS public website and related public digital services. By accessing or using these services, you agree to these terms.',
      'If you do not agree, do not use the public website or related public services described here.',
    ],
  },
  {
    id: 'service-terms',
    heading: 'Service terms',
    paragraphs: [
      'AOVIAS provides informational content, product descriptions, documentation, account experiences, and related public tools on an “as available” basis. We may update, suspend, or discontinue public features with reasonable care.',
      'You are responsible for maintaining the confidentiality of account credentials and for activity under your account.',
      'Public materials describe capability and direction honestly. They do not create product release dates, SLAs, or certifications unless a separate written agreement says so.',
    ],
  },
  {
    id: 'acceptable-use',
    heading: 'Acceptable use',
    paragraphs: [
      'You agree to use AOVIAS services lawfully and respectfully. Prohibited activities include attempts to disrupt services, unauthorized access, misuse of forms or automation, and distribution of harmful content.',
    ],
    bullets: [
      'Do not attempt to probe, scan, or breach systems outside authorized testing channels.',
      'Do not submit unlawful, deceptive, or abusive content through public forms.',
      'Do not interfere with other users’ access or with the integrity of the platform.',
      'Do not misrepresent your identity or affiliation when engaging AOVIAS.',
    ],
  },
  {
    id: 'intellectual-property',
    heading: 'Intellectual property',
    paragraphs: [
      'AOVIAS and its licensors own the trademarks, product names, software, documentation, and brand assets presented on this website, unless otherwise stated.',
      'You may not copy, modify, or redistribute AOVIAS materials except as expressly permitted in writing or through published licenses.',
    ],
  },
  {
    id: 'third-party',
    heading: 'Third-party services',
    paragraphs: [
      'The website may link to or integrate third-party services. Those services are governed by their own terms and privacy practices. AOVIAS is not responsible for third-party content or practices outside our control.',
    ],
  },
  {
    id: 'liability',
    heading: 'Liability',
    paragraphs: [
      'To the fullest extent permitted by law, AOVIAS is not liable for indirect, incidental, or consequential damages arising from use of the public website or reliance on public content.',
      'Nothing in these terms limits rights that cannot be limited under applicable law.',
    ],
  },
  {
    id: 'governing-law',
    heading: 'Governing law',
    paragraphs: [
      'These terms are intended to be interpreted under the laws applicable to AOVIAS’s principal place of business, without regard to conflict-of-law principles, unless a mandatory local law requires otherwise.',
      'If a provision is found unenforceable, the remaining provisions continue in effect.',
    ],
  },
  {
    id: 'contact',
    heading: 'Contact',
    paragraphs: [
      'Questions about these terms may be sent to legal@aovias.com or through the Contact page.',
    ],
  },
  {
    id: 'updates',
    heading: 'Updates to these terms',
    paragraphs: [
      'We may update this draft as services or legal requirements evolve. The version and last-updated date appear at the top of this page. Continued use after an update constitutes acceptance of the revised terms where permitted by law.',
    ],
  },
];
