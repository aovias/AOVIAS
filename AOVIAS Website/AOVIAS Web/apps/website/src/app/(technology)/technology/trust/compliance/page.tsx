import { JsonLdScript } from '@/components/seo';
import {
  TrustCompliancePage,
  trustComplianceBreadcrumbs,
  trustPagesSeo,
} from '@/features/technology';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';

const seo = trustPagesSeo.compliance;

export const metadata = createPageMetadata({
  title: seo.title,
  description: seo.description,
  path: seo.path,
  type: 'website',
  keywords: seo.keywords,
  image: '/og/technology.png',
  imageAlt: 'AOVIAS Compliance Readiness',
});

export default function TechnologyTrustComplianceRoute() {
  return (
    <>
      <JsonLdScript
        id="technology-trust-compliance"
        data={[
          ...buildPageSchemas({
            title: seo.title,
            description: seo.description,
            path: seo.path,
            includeOrganization: true,
            breadcrumbs: trustComplianceBreadcrumbs(),
          }),
        ]}
      />
      <TrustCompliancePage />
    </>
  );
}
