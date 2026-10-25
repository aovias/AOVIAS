import { JsonLdScript } from '@/components/seo';
import {
  buildIndustryCatalogSchema,
  SolutionsOverviewPage,
  solutionsSeo,
} from '@/features/industries';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';
import { ogImages } from '@/seo';

export const metadata = createPageMetadata({
  title: solutionsSeo.title,
  description: solutionsSeo.description,
  path: solutionsSeo.path,
  type: 'marketing',
  keywords: solutionsSeo.keywords,
  image: ogImages.solutions,
  imageAlt: 'AOVIAS Solutions',
});

/**
 * Solutions overview — Industry Experience Framework entry.
 */
export default function SolutionsRoutePage() {
  return (
    <>
      <JsonLdScript
        id="solutions-webpage"
        data={[
          ...buildPageSchemas({
            title: solutionsSeo.title,
            description: solutionsSeo.description,
            path: solutionsSeo.path,
            pageType: 'CollectionPage',
            includeOrganization: true,
            breadcrumbs: [
              { label: 'Home', href: '/' },
              { label: 'Solutions', href: solutionsSeo.path, current: true },
            ],
          }),
          buildIndustryCatalogSchema(),
        ]}
      />
      <SolutionsOverviewPage />
    </>
  );
}
