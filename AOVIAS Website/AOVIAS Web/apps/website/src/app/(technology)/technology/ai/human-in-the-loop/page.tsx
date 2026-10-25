import { JsonLdScript } from '@/components/seo';
import {
  AiHitlPage,
  aiHitlBreadcrumbs,
  aiPagesSeo,
} from '@/features/technology';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';

const seo = aiPagesSeo.hitl;

export const metadata = createPageMetadata({
  title: seo.title,
  description: seo.description,
  path: seo.path,
  type: 'website',
  keywords: seo.keywords,
  image: '/og/technology.png',
  imageAlt: 'AOVIAS Human-in-the-Loop',
});

export default function TechnologyAiHitlRoute() {
  return (
    <>
      <JsonLdScript
        id="technology-ai-hitl"
        data={[
          ...buildPageSchemas({
            title: seo.title,
            description: seo.description,
            path: seo.path,
            includeOrganization: true,
            breadcrumbs: aiHitlBreadcrumbs(),
          }),
        ]}
      />
      <AiHitlPage />
    </>
  );
}
