import { JsonLdScript } from '@/components/seo';
import {
  buildCareersJobPostingSchemas,
  CareersPage,
  careersSeo,
  getOpenRoles,
} from '@/features/careers';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';

export const metadata = createPageMetadata({
  title: careersSeo.title,
  description: careersSeo.description,
  path: careersSeo.path,
  type: 'marketing',
  keywords: careersSeo.keywords,
  image: '/og/careers.png',
  imageAlt: 'AOVIAS Careers',
});

/**
 * AOVIAS Careers — employer brand and open roles.
 * Layout: MarketingLayout → AppShell (via (company) route group).
 */
export default function CareersRoutePage() {
  const jobSchemas = buildCareersJobPostingSchemas(getOpenRoles());

  return (
    <>
      <JsonLdScript
        id="careers-webpage"
        data={buildPageSchemas({
          title: careersSeo.title,
          description: careersSeo.description,
          path: careersSeo.path,
          includeOrganization: true,
          breadcrumbs: [
            { label: 'Home', href: '/' },
            { label: 'Company', href: '/company' },
            { label: 'Careers', href: careersSeo.path, current: true },
          ],
        })}
      />
      {jobSchemas.length > 0 ? (
        <JsonLdScript id="careers-job-postings" data={jobSchemas} />
      ) : null}
      <CareersPage />
    </>
  );
}
