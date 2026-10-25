import { JsonLdScript } from '@/components/seo';
import {
  EngineeringArchitecturePage,
  engineeringArchitectureBreadcrumbs,
  engineeringPagesSeo,
} from '@/features/technology';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';

const seo = engineeringPagesSeo.architecture;

export const metadata = createPageMetadata({
  title: seo.title,
  description: seo.description,
  path: seo.path,
  type: 'website',
  keywords: seo.keywords,
  image: '/og/technology.png',
  imageAlt: 'AOVIAS Engineering Architecture',
});

export default function TechnologyEngineeringArchitectureRoute() {
  return (
    <>
      <JsonLdScript
        id="technology-engineering-architecture"
        data={[
          ...buildPageSchemas({
            title: seo.title,
            description: seo.description,
            path: seo.path,
            includeOrganization: true,
            breadcrumbs: engineeringArchitectureBreadcrumbs(),
          }),
        ]}
      />
      <EngineeringArchitecturePage />
    </>
  );
}
