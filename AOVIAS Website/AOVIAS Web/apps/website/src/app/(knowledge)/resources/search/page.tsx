import { Suspense } from 'react';

import { JsonLdScript } from '@/components/seo';
import {
  KnowledgeSearchPage,
  KnowledgeSearchView,
} from '@/features/knowledge';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';

const path = '/resources/search';

export const metadata = createPageMetadata({
  title: 'Search',
  description:
    'Search the AOVIAS Knowledge Platform across articles, insights, documentation, and resources.',
  path,
  type: 'marketing',
});

export default function KnowledgeSearchRoutePage() {
  return (
    <>
      <JsonLdScript
        id="knowledge-search"
        data={buildPageSchemas({
          title: 'Search',
          description: 'Knowledge search',
          path,
          breadcrumbs: [
            { label: 'Home', href: '/' },
            { label: 'Resources', href: '/resources' },
            { label: 'Search', href: path, current: true },
          ],
        })}
      />
      <Suspense fallback={<KnowledgeSearchView />}>
        <KnowledgeSearchPage />
      </Suspense>
    </>
  );
}
