import { JsonLdScript } from '@/components/seo';
import {
  TechnologyLandingPage,
  technologyLandingBreadcrumbs,
  technologyLandingSeo,
} from '@/features/technology';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';

export const metadata = createPageMetadata({
  title: technologyLandingSeo.title,
  description: technologyLandingSeo.description,
  path: technologyLandingSeo.path,
  type: 'website',
  keywords: technologyLandingSeo.keywords,
  image: '/og/technology.png',
  imageAlt: 'AOVIAS Technology',
});

/**
 * Technology section landing — engineering foundation overview.
 */
export default function TechnologyRoutePage() {
  return (
    <>
      <JsonLdScript
        id="technology-webpage"
        data={[
          ...buildPageSchemas({
            title: technologyLandingSeo.title,
            description: technologyLandingSeo.description,
            path: technologyLandingSeo.path,
            pageType: 'CollectionPage',
            includeOrganization: true,
            breadcrumbs: technologyLandingBreadcrumbs(),
          }),
        ]}
      />
      <TechnologyLandingPage />
    </>
  );
}
