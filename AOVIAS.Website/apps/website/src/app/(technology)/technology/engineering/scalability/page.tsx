import { JsonLdScript } from '@/components/seo';
import {
  EngineeringScalabilityPage,
  engineeringPagesSeo,
  engineeringScalabilityBreadcrumbs,
} from '@/features/technology';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';

const seo = engineeringPagesSeo.scalability;

export const metadata = createPageMetadata({
  title: seo.title,
  description: seo.description,
  path: seo.path,
  type: 'website',
  keywords: seo.keywords,
  image: '/og/technology.png',
  imageAlt: 'AOVIAS Scalability',
});

export default function TechnologyEngineeringScalabilityRoute() {
  return (
    <>
      <JsonLdScript
        id="technology-engineering-scalability"
        data={[
          ...buildPageSchemas({
            title: seo.title,
            description: seo.description,
            path: seo.path,
            includeOrganization: true,
            breadcrumbs: engineeringScalabilityBreadcrumbs(),
          }),
        ]}
      />
      <EngineeringScalabilityPage />
    </>
  );
}
