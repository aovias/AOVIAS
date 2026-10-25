import { JsonLdScript } from '@/components/seo';
import {
  PlatformServicesPage,
  platformPagesSeo,
  platformServicesBreadcrumbs,
} from '@/features/technology';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';

const seo = platformPagesSeo.services;

export const metadata = createPageMetadata({
  title: seo.title,
  description: seo.description,
  path: seo.path,
  type: 'website',
  keywords: seo.keywords,
  image: '/og/technology.png',
  imageAlt: 'AOVIAS Platform Services',
});

export default function TechnologyPlatformServicesRoute() {
  return (
    <>
      <JsonLdScript
        id="technology-platform-services"
        data={[
          ...buildPageSchemas({
            title: seo.title,
            description: seo.description,
            path: seo.path,
            includeOrganization: true,
            breadcrumbs: platformServicesBreadcrumbs(),
          }),
        ]}
      />
      <PlatformServicesPage />
    </>
  );
}
