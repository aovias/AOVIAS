import { JsonLdScript } from '@/components/seo';
import {
  TrustDataProtectionPage,
  trustDataProtectionBreadcrumbs,
  trustPagesSeo,
} from '@/features/technology';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';

const seo = trustPagesSeo.dataProtection;

export const metadata = createPageMetadata({
  title: seo.title,
  description: seo.description,
  path: seo.path,
  type: 'website',
  keywords: seo.keywords,
  image: '/og/technology.png',
  imageAlt: 'AOVIAS Data Protection',
});

export default function TechnologyTrustDataProtectionRoute() {
  return (
    <>
      <JsonLdScript
        id="technology-trust-data-protection"
        data={[
          ...buildPageSchemas({
            title: seo.title,
            description: seo.description,
            path: seo.path,
            includeOrganization: true,
            breadcrumbs: trustDataProtectionBreadcrumbs(),
          }),
        ]}
      />
      <TrustDataProtectionPage />
    </>
  );
}