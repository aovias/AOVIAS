import { JsonLdScript } from '@/components/seo';
import {
  DeveloperPlatformPage,
  developerBreadcrumbs,
  ecosystemPagesSeo,
} from '@/features/technology';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';

const seo = ecosystemPagesSeo.developer;

export const metadata = createPageMetadata({
  title: seo.title,
  description: seo.description,
  path: seo.path,
  type: 'website',
  keywords: seo.keywords,
  image: '/og/technology.png',
  imageAlt: 'AOVIAS Developer Platform',
});

export default function TechnologyDeveloperRoute() {
  return (
    <>
      <JsonLdScript
        id="technology-developer"
        data={[
          ...buildPageSchemas({
            title: seo.title,
            description: seo.description,
            path: seo.path,
            includeOrganization: true,
            breadcrumbs: developerBreadcrumbs(),
          }),
        ]}
      />
      <DeveloperPlatformPage />
    </>
  );
}
