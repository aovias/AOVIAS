import { seoConfig } from '@/config/seo';
import type { OpenRole } from '@/features/careers/domain';
import { buildAbsoluteUrl, buildCanonicalUrl } from '@/lib/seo/canonical';
import {
  buildOrganizationReference,
  type JsonLd,
} from '@/lib/seo/schema';

export const careersSeo = {
  title: 'Careers — Engineering with Institutional Purpose',
  description:
    'Join AOVIAS to build dependable technology that strengthens institutions — craftsmanship, ownership, and long-term engineering excellence.',
  path: '/company/careers',
  keywords: [
    'AOVIAS careers',
    'AOVIAS jobs',
    'engineering careers',
    'responsible technology jobs',
    'platform engineer',
    'institutional software',
  ],
} as const;

const employmentTypeSchema: Record<OpenRole['employmentType'], string> = {
  'full-time': 'FULL_TIME',
  'part-time': 'PART_TIME',
  contract: 'CONTRACTOR',
  internship: 'INTERN',
};

/**
 * JobPosting schema readiness — emit for open (and optionally upcoming) roles.
 */
export function buildJobPostingSchema(role: OpenRole): JsonLd {
  const posting: JsonLd = {
    '@context': 'https://schema.org',
    '@type': 'JobPosting',
    title: role.title,
    description: role.description,
    datePosted: role.publishedAt,
    employmentType: employmentTypeSchema[role.employmentType],
    identifier: {
      '@type': 'PropertyValue',
      name: 'AOVIAS',
      value: role.id,
    },
    hiringOrganization: {
      ...buildOrganizationReference(),
      name: seoConfig.organization.name,
      sameAs: seoConfig.organization.url,
      logo: buildAbsoluteUrl(seoConfig.organization.logoPath),
    },
    url: `${buildCanonicalUrl(careersSeo.path)}#role-${role.slug}`,
    directApply: false,
  };

  if (role.remote) {
    posting['jobLocationType'] = 'TELECOMMUTE';
  }

  posting['jobLocation'] = {
    '@type': 'Place',
    address: {
      '@type': 'PostalAddress',
      addressLocality: role.location,
      ...(role.addressCountry ? { addressCountry: role.addressCountry } : {}),
    },
  };

  return posting;
}

export function buildCareersJobPostingSchemas(
  roles: readonly OpenRole[],
): JsonLd[] {
  return roles
    .filter((role) => role.status === 'open' || role.status === 'upcoming')
    .map(buildJobPostingSchema);
}
