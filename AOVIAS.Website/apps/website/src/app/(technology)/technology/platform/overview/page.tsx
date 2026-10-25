import { JsonLdScript } from '@/components/seo';
import {
  PlatformOverviewPage,
  platformOverviewBreadcrumbs,
  platformPagesSeo,
} from '@/features/technology';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';

const seo = platformPagesSeo.overview;

export const metadata = createPageMetadata({
  title: seo.title,
  description: seo.description,
  path: seo.path,
  type: 'website',
  keywords: seo.keywords,
  image: '/og/technology.png',
  imageAlt: 'AOVIAS Platform Overview',
});

export default function TechnologyPlatformOverviewRoute() {
  return (
    <>
      <JsonLdScript
        id="technology-platform-overview"
        data={[
          ...buildPageSchemas({
            title: seo.title,
            description: seo.description,
            path: seo.path,
            includeOrganization: true,
            breadcrumbs: platformOverviewBreadcrumbs(),
          }),
        ]}
      />
      <PlatformOverviewPage />
    </>
  );
}
