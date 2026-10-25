import { JsonLdScript } from '@/components/seo';
import { KnowledgeHubPage, knowledgeHubSeo } from '@/features/knowledge';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';

export const metadata = createPageMetadata({
  title: knowledgeHubSeo.title,
  description: knowledgeHubSeo.description,
  path: knowledgeHubSeo.path,
  type: 'marketing',
  keywords: knowledgeHubSeo.keywords,
  image: '/og/resources.png',
  imageAlt: 'AOVIAS Resources',
});

export default function ResourcesHubRoutePage() {
  return (
    <>
      <JsonLdScript
        id="knowledge-hub"
        data={buildPageSchemas({
          title: knowledgeHubSeo.title,
          description: knowledgeHubSeo.description,
          path: knowledgeHubSeo.path,
          pageType: 'CollectionPage',
          includeOrganization: true,
          breadcrumbs: [
            { label: 'Home', href: '/' },
            { label: 'Resources', href: '/resources', current: true },
          ],
        })}
      />
      <KnowledgeHubPage />
    </>
  );
}
