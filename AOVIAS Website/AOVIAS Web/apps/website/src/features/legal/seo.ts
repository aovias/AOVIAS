/**
 * Trust & Governance SEO — owned by the Legal feature, consumed by routes.
 */
export const legalHubSeo = {
  title: 'Trust & Governance',
  description:
    'AOVIAS Trust & Governance Center — privacy, terms, cookies, accessibility, and security commitments for the public website.',
  path: '/legal',
  keywords: [
    'AOVIAS trust',
    'governance',
    'privacy',
    'terms of service',
    'accessibility',
    'security',
  ],
} as const;

export const legalDocumentSeo = {
  privacy: {
    title: 'Privacy Policy',
    description:
      'How AOVIAS collects, uses, and protects personal information on the public website and related services.',
    path: '/legal/privacy',
    keywords: [
      'AOVIAS privacy policy',
      'data protection',
      'personal information',
    ],
  },
  terms: {
    title: 'Terms of Service',
    description:
      'Terms governing use of the AOVIAS public website, including acceptable use, intellectual property, and liability.',
    path: '/legal/terms',
    keywords: ['AOVIAS terms of service', 'acceptable use', 'website terms'],
  },
  cookies: {
    title: 'Cookie Policy',
    description:
      'Cookie categories, usage, preferences, and consent information for the AOVIAS public website.',
    path: '/legal/cookies',
    keywords: ['AOVIAS cookie policy', 'cookies', 'preferences'],
  },
  accessibility: {
    title: 'Accessibility Statement',
    description:
      'AOVIAS accessibility commitment, WCAG alignment, known limitations, and feedback process.',
    path: '/legal/accessibility',
    keywords: ['AOVIAS accessibility', 'WCAG 2.2', 'accessibility statement'],
  },
  security: {
    title: 'Security Overview',
    description:
      'AOVIAS security principles, responsible disclosure, and high-level infrastructure posture.',
    path: '/legal/security',
    keywords: [
      'AOVIAS security',
      'responsible disclosure',
      'security overview',
    ],
  },
} as const;
