import { JsonLdScript } from '@/components/seo';
import {
  libraryEditorialContent,
  KnowledgeEditorialTemplate,
} from '@/features/knowledge/editorial';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';

const path = '/resources/library';

export const metadata = createPageMetadata({
  title: 'Resources — Enterprise Knowledge Library',
  description:
    'AOVIAS Resources — guides, checklists, and reference material for institutional evaluation and adoption.',
  path,
  type: 'marketing',
  keywords: ['AOVIAS resources', 'guides', 'reference library'],
});

export default function LibraryIndexPage() {
  return (
    <>
      <JsonLdScript
        id="library-index"
        data={buildPageSchemas({
          title: 'Resource library',
          description: 'AOVIAS resource library',
          path,
          breadcrumbs: [
            { label: 'Home', href: '/' },
            { label: 'Resources', href: '/resources' },
            { label: 'Library', href: path, current: true },
          ],
        })}
      />
      <KnowledgeEditorialTemplate content={libraryEditorialContent} />
    </>
  );
}
