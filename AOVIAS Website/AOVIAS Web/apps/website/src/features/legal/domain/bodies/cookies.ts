import type { LegalBodySection } from '@/features/legal/types';

/**
 * Cookie Policy body — public draft for counsel review.
 * Not legal advice; not counsel-approved.
 */
export const cookiesBody: readonly LegalBodySection[] = [
  {
    id: 'notice',
    heading: 'Public draft notice',
    paragraphs: [
      'This Cookie Policy is a public draft published for transparency and counsel review. It is not legal advice and is not counsel-approved. It complements the Privacy Policy.',
    ],
  },
  {
    id: 'introduction',
    heading: 'Introduction',
    paragraphs: [
      'This policy explains how AOVIAS uses cookies and similar technologies on the public website.',
      'A dedicated in-product consent management interface is not yet part of this experience. Until it is, browser controls are the primary way to manage cookies, as described below.',
    ],
  },
  {
    id: 'what-are-cookies',
    heading: 'What cookies are',
    paragraphs: [
      'Cookies are small text files stored on your device. Similar technologies may include local storage or pixels used for the same operational purposes described here.',
      'Some cookies are set by AOVIAS. Others may be set by providers that help us deliver the site when those integrations are enabled.',
    ],
  },
  {
    id: 'cookie-categories',
    heading: 'Cookie categories',
    paragraphs: [
      'We group cookies by purpose so you can understand why they may be used.',
    ],
    bullets: [
      'Essential — required for security, load balancing, session continuity, and basic site operation.',
      'Preferences — remember settings such as theme or locale when available.',
      'Analytics — help us understand aggregated usage to improve clarity and performance, used only when enabled.',
      'Marketing — not a primary focus of the AOVIAS public website. Any such cookies would be disclosed and controllable when introduced.',
    ],
  },
  {
    id: 'usage',
    heading: 'How cookies are used',
    paragraphs: [
      'Essential cookies support reliable delivery of pages, forms, and authenticated experiences. Preference cookies improve continuity across visits.',
      'Analytics cookies, when present, are used to understand trends — not to build invasive personal profiles for sale.',
    ],
  },
  {
    id: 'preferences',
    heading: 'Preferences and controls',
    paragraphs: [
      'You can control cookies through your browser settings, including blocking or deleting cookies. Blocking essential cookies may affect site functionality.',
      'When a consent management experience ships, this policy will describe how to update preferences on the site. Until then, browser controls remain the primary mechanism.',
    ],
  },
  {
    id: 'consent-information',
    heading: 'Consent',
    paragraphs: [
      'Where consent is required by applicable law, AOVIAS will seek it before non-essential cookies are set. Essential cookies may be used as needed to provide the service you request.',
      'For questions about cookies or privacy, contact privacy@aovias.com.',
    ],
  },
  {
    id: 'updates',
    heading: 'Updates to this policy',
    paragraphs: [
      'We may update this draft as cookie practices or legal requirements evolve. The version and last-updated date appear at the top of this page.',
    ],
  },
];
