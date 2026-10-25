import { JsonLdScript } from '@/components/seo';
import {
  AiRoadmapPage,
  aiPagesSeo,
  aiRoadmapBreadcrumbs,
} from '@/features/technology';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';

const seo = aiPagesSeo.roadmap;

export const metadata = createPageMetadata({
  title: seo.title,
  description: seo.description,
  path: seo.path,
  type: 'website',
  keywords: seo.keywords,
  image: '/og/technology.png',
  imageAlt: 'AOVIAS AI Roadmap',
});

export default function TechnologyAiRoadmapRoute() {
  return (
    <>
      <JsonLdScript
        id="technology-ai-roadmap"
        data={[
          ...buildPageSchemas({
            title: seo.title,
            description: seo.description,
            path: seo.path,
            includeOrganization: true,
            breadcrumbs: aiRoadmapBreadcrumbs(),
          }),
        ]}
      />
      <AiRoadmapPage />
    </>
  );
}
