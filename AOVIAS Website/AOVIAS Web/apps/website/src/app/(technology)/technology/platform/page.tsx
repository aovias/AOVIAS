import { JsonLdScript } from '@/components/seo';
import {
  PlatformHubPage,
  platformHubBreadcrumbs,
  platformPagesSeo,
} from '@/features/technology';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';

const seo = platformPagesSeo.hub;

export const metadata = createPageMetadata({
  title: seo.title,
  description: seo.description,
  path: seo.path,
  type: 'website',
  keywords: seo.keywords,
  image: '/og/technology.png',
  imageAlt: 'AOVIAS Platform',
});

export default function TechnologyPlatformHubRoute() {
  return (
    <>
      <JsonLdScript
        id="technology-platform-hub"
        data={[
          ...buildPageSchemas({
            title: seo.title,
            description: seo.description,
            path: seo.path,
            includeOrganization: true,
            breadcrumbs: platformHubBreadcrumbs(),
          }),
        ]}
      />
      <PlatformHubPage />
    </>
  );
}
