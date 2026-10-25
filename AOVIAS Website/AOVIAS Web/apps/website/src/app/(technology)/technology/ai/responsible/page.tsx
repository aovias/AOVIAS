import { JsonLdScript } from '@/components/seo';
import {
  AiResponsiblePage,
  aiPagesSeo,
  aiResponsibleBreadcrumbs,
} from '@/features/technology';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';

const seo = aiPagesSeo.responsible;

export const metadata = createPageMetadata({
  title: seo.title,
  description: seo.description,
  path: seo.path,
  type: 'website',
  keywords: seo.keywords,
  image: '/og/technology.png',
  imageAlt: 'AOVIAS Responsible AI',
});

export default function TechnologyAiResponsibleRoute() {
  return (
    <>
      <JsonLdScript
        id="technology-ai-responsible"
        data={[
          ...buildPageSchemas({
            title: seo.title,
            description: seo.description,
            path: seo.path,
            includeOrganization: true,
            breadcrumbs: aiResponsibleBreadcrumbs(),
          }),
        ]}
      />
      <AiResponsiblePage />
    </>
  );
}
