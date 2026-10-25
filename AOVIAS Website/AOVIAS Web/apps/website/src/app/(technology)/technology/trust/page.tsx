import { JsonLdScript } from '@/components/seo';
import {
  TrustSecurityPage,
  trustPagesSeo,
  trustSecurityBreadcrumbs,
} from '@/features/technology';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';

const seo = trustPagesSeo.security;

export const metadata = createPageMetadata({
  title: seo.title,
  description: seo.description,
  path: seo.path,
  type: 'website',
  keywords: seo.keywords,
  image: '/og/technology.png',
  imageAlt: 'AOVIAS Security',
});

export default function TechnologyTrustSecurityRoute() {
  return (
    <>
      <JsonLdScript
        id="technology-trust-security"
        data={[
          ...buildPageSchemas({
            title: seo.title,
            description: seo.description,
            path: seo.path,
            includeOrganization: true,
            breadcrumbs: trustSecurityBreadcrumbs(),
          }),
        ]}
      />
      <TrustSecurityPage />
    </>
  );
}
