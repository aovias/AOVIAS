import { JsonLdScript } from '@/components/seo';
import {
  blogEditorialContent,
  KnowledgeEditorialTemplate,
} from '@/features/knowledge/editorial';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';

const path = '/resources/blog';

export const metadata = createPageMetadata({
  title: 'Blog — Enterprise Knowledge',
  description:
    'AOVIAS Blog — thought leadership, engineering culture, and institutional technology writing.',
  path,
  type: 'marketing',
  keywords: ['AOVIAS blog', 'engineering', 'thought leadership'],
  image: '/og/blog.png',
  imageAlt: 'AOVIAS Blog',
});

export default function BlogIndexPage() {
  return (
    <>
      <JsonLdScript
        id="blog-index"
        data={buildPageSchemas({
          title: 'Blog',
          description: 'AOVIAS Blog — enterprise knowledge',
          path,
          breadcrumbs: [
            { label: 'Home', href: '/' },
            { label: 'Resources', href: '/resources' },
            { label: 'Blog', href: path, current: true },
          ],
        })}
      />
      <KnowledgeEditorialTemplate content={blogEditorialContent} />
    </>
  );
}
