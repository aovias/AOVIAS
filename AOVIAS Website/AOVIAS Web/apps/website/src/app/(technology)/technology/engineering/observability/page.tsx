import { JsonLdScript } from '@/components/seo';
import {
  EngineeringObservabilityPage,
  engineeringObservabilityBreadcrumbs,
  engineeringPagesSeo,
} from '@/features/technology';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';

const seo = engineeringPagesSeo.observability;

export const metadata = createPageMetadata({
  title: seo.title,
  description: seo.description,
  path: seo.path,
  type: 'website',
  keywords: seo.keywords,
  image: '/og/technology.png',
  imageAlt: 'AOVIAS Observability',
});

export default function TechnologyEngineeringObservabilityRoute() {
  return (
    <>
      <JsonLdScript
        id="technology-engineering-observability"
        data={[
          ...buildPageSchemas({
            title: seo.title,
            description: seo.description,
            path: seo.path,
            includeOrganization: true,
            breadcrumbs: engineeringObservabilityBreadcrumbs(),
          }),
        ]}
      />
      <EngineeringObservabilityPage />
    </>
  );
}