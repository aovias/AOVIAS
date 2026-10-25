import type { ReactNode } from 'react';

import { KnowledgeLayout } from '@/layouts/knowledge/knowledge-layout';

/** Editorial knowledge routes — Blog, Insights, Library, Categories, Search, Hub. */
export default function KnowledgeRouteLayout({
  children,
}: {
  children: ReactNode;
}) {
  return <KnowledgeLayout>{children}</KnowledgeLayout>;
}
