import { JsonLdScript } from '@/components/seo';
import { TrustHubPage, legalHubSeo } from '@/features/legal';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';

export const metadata = createPageMetadata({
  title: legalHubSeo.title,
  description: legalHubSeo.description,
  path: legalHubSeo.path,
  type: 'legal',
  keywords: legalHubSeo.keywords,
});

/**
 * Trust & Governance Center hub.
 * Layout: LegalLayout → AppShell (via (legal) route group).
 */
export default function LegalHubRoutePage() {
  return (
    <>
      <JsonLdScript
        id="legal-hub-webpage"
        data={buildPageSchemas({
          title: legalHubSeo.title,
          description: legalHubSeo.description,
          path: legalHubSeo.path,
          includeOrganization: true,
          breadcrumbs: [
            { label: 'Home', href: '/' },
            { label: 'Trust & Governance', href: legalHubSeo.path, current: true },
          ],
        })}
      />
      <TrustHubPage />
    </>
  );
}
