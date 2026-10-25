import { seoConfig } from '@/config/seo';
import { companyLegal } from '@/config/company-legal';
import { buildAbsoluteUrl, buildCanonicalUrl } from '@/lib/seo/canonical';
import { organizationNodeId, type JsonLd } from '@/lib/seo/schema';

export const contactSeo = {
  title: 'Contact — Demo, Enterprise & Partnerships',
  description:
    'Request a demo, contact enterprise, or talk with AOVIAS — clear engagement paths for institutions and partners.',
  path: '/company/contact',
  keywords: [
    'AOVIAS contact',
    'AOVIAS demo',
    'enterprise inquiry',
    'AOVIAS partnership',
    'request a demo',
  ],
} as const;

/**
 * ContactPoint schema — merges onto the canonical Organization via @id.
 * Root layout emits the full Organization; this adds engagement channels.
 */
export function buildContactPointSchemas(): JsonLd[] {
  const org = seoConfig.organization;
  const email = org.contactEmail;

  return [
    {
      '@context': 'https://schema.org',
      '@type': 'Organization',
      '@id': organizationNodeId(),
      name: org.name,
      url: org.url,
      logo: buildAbsoluteUrl(org.logoPath),
      email,
      contactPoint: [
        {
          '@type': 'ContactPoint',
          contactType: 'sales',
          email: companyLegal.enquiryEmail,
          telephone: companyLegal.phoneTel,
          url: buildCanonicalUrl(`${contactSeo.path}#form-contact`),
          availableLanguage: ['English'],
        },
        {
          '@type': 'ContactPoint',
          contactType: 'customer support',
          email,
          telephone: companyLegal.phoneTel,
          url: buildCanonicalUrl(`${contactSeo.path}#form-contact`),
          availableLanguage: ['English'],
        },
        {
          '@type': 'ContactPoint',
          contactType: 'partnerships',
          email,
          url: buildCanonicalUrl(`${contactSeo.path}#form-partner`),
          availableLanguage: ['English'],
        },
      ],
    },
  ];
}
