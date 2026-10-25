import { notFound } from 'next/navigation';

import { JsonLdScript } from '@/components/seo';
import {
  ArticleDetailTemplate,
  buildArticleJsonLd,
  getKnowledgeByKind,
  getKnowledgeDetail,
} from '@/features/knowledge';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';

type Props = { params: Promise<{ slug: string }> };

export function generateStaticParams() {
  return getKnowledgeByKind(['insight']).map((item) => ({
    slug: item.href.split('/').pop()!,
  }));
}

export async function generateMetadata({ params }: Props) {
  const { slug } = await params;
  const content = getKnowledgeDetail(slug);
  if (!content || content.item.kind !== 'insight') return {};
  return createPageMetadata({
    title: content.seo.title,
    description: content.seo.description,
    path: content.item.href,
    type: 'marketing',
    ...(content.seo.keywords ? { keywords: content.seo.keywords } : {}),
  });
}

export default async function InsightArticlePage({ params }: Props) {
  const { slug } = await params;
  const content = getKnowledgeDetail(slug);
  if (!content || content.item.kind !== 'insight') notFound();

  return (
    <>
      <JsonLdScript
        id={`insight-${slug}`}
        data={[
          ...buildPageSchemas({
            title: content.seo.title,
            description: content.seo.description,
            path: content.item.href,
            breadcrumbs: [
              { label: 'Home', href: '/' },
              { label: 'Resources', href: '/resources' },
              { label: 'Insights', href: '/resources/insights' },
              { label: content.item.title, href: content.item.href, current: true },
            ],
          }),
          buildArticleJsonLd(content.item),
        ]}
      />
      <ArticleDetailTemplate content={content} sectionKinds={['insight']} />
    </>
  );
}
