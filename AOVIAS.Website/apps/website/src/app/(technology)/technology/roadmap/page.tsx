import { JsonLdScript } from '@/components/seo';
import {
  TechnologyRoadmapPage,
  ecosystemPagesSeo,
  roadmapBreadcrumbs,
} from '@/features/technology';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';

const seo = ecosystemPagesSeo.roadmap;

export const metadata = createPageMetadata({
  title: seo.title,
  description: seo.description,
  path: seo.path,
  type: 'website',
  keywords: seo.keywords,
  image: '/og/technology.png',
  imageAlt: 'AOVIAS Technology Roadmap',
});

export default function TechnologyRoadmapRoute() {
  return (
    <>
      <JsonLdScript
        id="technology-roadmap"
        data={[
          ...buildPageSchemas({
            title: seo.title,
            description: seo.description,
            path: seo.path,
            includeOrganization: true,
            breadcrumbs: roadmapBreadcrumbs(),
          }),
        ]}
      />
      <TechnologyRoadmapPage />
    </>
  );
}
