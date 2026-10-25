import { JsonLdScript } from '@/components/seo';
import {
  EngineeringHubPage,
  engineeringHubBreadcrumbs,
  engineeringPagesSeo,
} from '@/features/technology';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';

const seo = engineeringPagesSeo.hub;

export const metadata = createPageMetadata({
  title: seo.title,
  description: seo.description,
  path: seo.path,
  type: 'website',
  keywords: seo.keywords,
  image: '/og/technology.png',
  imageAlt: 'AOVIAS Engineering Philosophy',
});

export default function TechnologyEngineeringHubRoute() {
  return (
    <>
      <JsonLdScript
        id="technology-engineering-hub"
        data={[
          ...buildPageSchemas({
            title: seo.title,
            description: seo.description,
            path: seo.path,
            includeOrganization: true,
            breadcrumbs: engineeringHubBreadcrumbs(),
          }),
        ]}
      />
      <EngineeringHubPage />
    </>
  );
}
