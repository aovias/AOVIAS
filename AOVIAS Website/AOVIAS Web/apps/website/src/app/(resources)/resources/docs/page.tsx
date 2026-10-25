import { JsonLdScript } from '@/components/seo';
import {
  docsEditorialContent,
  KnowledgeEditorialTemplate,
} from '@/features/knowledge/editorial';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';

const path = '/resources/docs';

export const metadata = createPageMetadata({
  title: 'Documentation — Enterprise Knowledge',
  description:
    'AOVIAS Documentation — structured product documentation, architecture, and implementation guidance.',
  path,
  type: 'marketing',
  keywords: ['AOVIAS documentation', 'product docs', 'architecture guides'],
  image: '/og/documentation.png',
  imageAlt: 'AOVIAS Documentation',
});

export default function DocsIndexPage() {
  return (
    <>
      <JsonLdScript
        id="docs-index"
        data={buildPageSchemas({
          title: 'Documentation',
          description: 'AOVIAS Documentation — enterprise knowledge',
          path,
          breadcrumbs: [
            { label: 'Home', href: '/' },
            { label: 'Resources', href: '/resources' },
            { label: 'Documentation', href: path, current: true },
          ],
        })}
      />
      <KnowledgeEditorialTemplate content={docsEditorialContent} />
    </>
  );
}
