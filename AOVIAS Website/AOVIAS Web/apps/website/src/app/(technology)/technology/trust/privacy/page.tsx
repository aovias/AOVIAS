import { JsonLdScript } from '@/components/seo';
import {
  TrustPrivacyPage,
  trustPagesSeo,
  trustPrivacyBreadcrumbs,
} from '@/features/technology';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';

const seo = trustPagesSeo.privacy;

export const metadata = createPageMetadata({
  title: seo.title,
  description: seo.description,
  path: seo.path,
  type: 'website',
  keywords: seo.keywords,
  image: '/og/technology.png',
  imageAlt: 'AOVIAS Privacy & Trust',
});

export default function TechnologyTrustPrivacyRoute() {
  return (
    <>
      <JsonLdScript
        id="technology-trust-privacy"
        data={[
          ...buildPageSchemas({
            title: seo.title,
            description: seo.description,
            path: seo.path,
            includeOrganization: true,
            breadcrumbs: trustPrivacyBreadcrumbs(),
          }),
        ]}
      />
      <TrustPrivacyPage />
    </>
  );
}
