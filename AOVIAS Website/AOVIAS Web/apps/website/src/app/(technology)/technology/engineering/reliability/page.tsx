import { JsonLdScript } from '@/components/seo';
import {
  EngineeringReliabilityPage,
  engineeringPagesSeo,
  engineeringReliabilityBreadcrumbs,
} from '@/features/technology';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';

const seo = engineeringPagesSeo.reliability;

export const metadata = createPageMetadata({
  title: seo.title,
  description: seo.description,
  path: seo.path,
  type: 'website',
  keywords: seo.keywords,
  image: '/og/technology.png',
  imageAlt: 'AOVIAS Reliability',
});

export default function TechnologyEngineeringReliabilityRoute() {
  return (
    <>
      <JsonLdScript
        id="technology-engineering-reliability"
        data={[
          ...buildPageSchemas({
            title: seo.title,
            description: seo.description,
            path: seo.path,
            includeOrganization: true,
            breadcrumbs: engineeringReliabilityBreadcrumbs(),
          }),
        ]}
      />
      <EngineeringReliabilityPage />
    </>
  );
}
