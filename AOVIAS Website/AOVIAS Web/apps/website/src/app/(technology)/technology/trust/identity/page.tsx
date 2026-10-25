import { JsonLdScript } from '@/components/seo';
import {
  TrustIdentityPage,
  trustIdentityBreadcrumbs,
  trustPagesSeo,
} from '@/features/technology';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';

const seo = trustPagesSeo.identity;

export const metadata = createPageMetadata({
  title: seo.title,
  description: seo.description,
  path: seo.path,
  type: 'website',
  keywords: seo.keywords,
  image: '/og/technology.png',
  imageAlt: 'AOVIAS Identity Platform',
});

export default function TechnologyTrustIdentityRoute() {
  return (
    <>
      <JsonLdScript
        id="technology-trust-identity"
        data={[
          ...buildPageSchemas({
            title: seo.title,
            description: seo.description,
            path: seo.path,
            includeOrganization: true,
            breadcrumbs: trustIdentityBreadcrumbs(),
          }),
        ]}
      />
      <TrustIdentityPage />
    </>
  );
}
