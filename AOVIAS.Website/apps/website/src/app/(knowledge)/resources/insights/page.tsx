import { JsonLdScript } from '@/components/seo';
import {
  insightsEditorialContent,
  KnowledgeEditorialTemplate,
} from '@/features/knowledge/editorial';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';

const path = '/resources/insights';

export const metadata = createPageMetadata({
  title: 'Insights — Enterprise Knowledge',
  description:
    'AOVIAS Insights — research, architecture thinking, and executive perspectives for institutional leaders.',
  path,
  type: 'marketing',
  keywords: ['AOVIAS insights', 'architecture thinking', 'responsible AI'],
});

export default function InsightsIndexPage() {
  return (
    <>
      <JsonLdScript
        id="insights-index"
        data={buildPageSchemas({
          title: 'Insights',
          description: 'AOVIAS Insights — enterprise knowledge',
          path,
          breadcrumbs: [
            { label: 'Home', href: '/' },
            { label: 'Resources', href: '/resources' },
            { label: 'Insights', href: path, current: true },
          ],
        })}
      />
      <KnowledgeEditorialTemplate content={insightsEditorialContent} />
    </>
  );
}
