import type { LegalBodySection } from '@/features/legal/types';

/**
 * Security Overview body — public draft for counsel review.
 * Principles only; no sensitive implementation detail; no invented certifications.
 */
export const securityBody: readonly LegalBodySection[] = [
  {
    id: 'notice',
    heading: 'Public draft notice',
    paragraphs: [
      'This Security Overview is a public draft published for transparency and counsel review. It is not legal advice and is not counsel-approved. It does not claim audit results or certification marks.',
    ],
  },
  {
    id: 'security-principles',
    heading: 'Security principles',
    paragraphs: [
      'AOVIAS approaches security as a continuous practice aligned with institutional trust. We prioritize least privilege, secure defaults, and responsible handling of personal and operational data.',
      'This overview is intentionally high level. It does not disclose internal architecture, tooling, or controls that would reduce security if published.',
    ],
  },
  {
    id: 'what-we-protect',
    heading: 'What we protect',
    paragraphs: [
      'We focus protection on public services, account and identity experiences, personal information submitted through the website, and the integrity of institutional engagements that begin online.',
      'Security and stewardship are treated as fundamentals — measured claims, clear ownership, and careful disclosure.',
    ],
  },
  {
    id: 'responsible-disclosure',
    heading: 'Responsible disclosure',
    paragraphs: [
      'If you believe you have found a security vulnerability in an AOVIAS public service, please report it responsibly. Provide enough detail for reproduction without exploiting the issue beyond what is necessary to demonstrate impact.',
      'Send reports to security@aovias.com. We ask that you allow a reasonable time for investigation before public disclosure.',
    ],
    bullets: [
      'Describe the affected URL or service and steps to reproduce.',
      'Avoid accessing data that is not yours.',
      'Do not disrupt availability as part of testing.',
    ],
  },
  {
    id: 'infrastructure-overview',
    heading: 'Infrastructure overview',
    paragraphs: [
      'Public services are delivered through modern cloud-hosted environments with encrypted transport, segmented access, and operational monitoring.',
      'We do not publish network diagrams, vendor inventories, or internal runbooks on this page.',
    ],
  },
  {
    id: 'no-certifications-claimed',
    heading: 'Certifications and audits',
    paragraphs: [
      'This page does not list certification marks or audit results. When counsel-approved claims are available, they will be published deliberately — not invented for marketing effect.',
    ],
  },
  {
    id: 'contact-information',
    heading: 'Contact',
    paragraphs: [
      'For security reports, use security@aovias.com. For privacy matters, see the Privacy Policy. For general inquiries, use the Contact page.',
    ],
  },
];
