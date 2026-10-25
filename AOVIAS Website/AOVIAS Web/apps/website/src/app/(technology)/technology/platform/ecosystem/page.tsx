import { JsonLdScript } from '@/components/seo';
import {
  PlatformEcosystemPage,
  platformEcosystemBreadcrumbs,
  platformPagesSeo,
} from '@/features/technology';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';

const seo = platformPagesSeo.ecosystem;

export const metadata = createPageMetadata({
  title: seo.title,
  description: seo.description,
  path: seo.path,
  type: 'website',
  keywords: seo.keywords,
  image: '/og/technology.png',
  imageAlt: 'AOVIAS Platform Ecosystem',
});

export default function TechnologyPlatformEcosystemRoute() {
  return (
    <>
      <JsonLdScript
        id="technology-platform-ecosystem"
        data={[
          ...buildPageSchemas({
            title: seo.title,
            description: seo.description,
            path: seo.path,
            includeOrganization: true,
            breadcrumbs: platformEcosystemBreadcrumbs(),
          }),
        ]}
      />
      <PlatformEcosystemPage />
    </>
  );
}
