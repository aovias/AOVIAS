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
  return getKnowledgeByKind(['article']).map((item) => ({
    slug: item.href.split('/').pop()!,
  }));
}

export async function generateMetadata({ params }: Props) {
  const { slug } = await params;
  const content = getKnowledgeDetail(slug);
  if (!content || content.item.kind !== 'article') return {};
  return createPageMetadata({
    title: content.seo.title,
    description: content.seo.description,
    path: content.item.href,
    type: 'marketing',
    ...(content.seo.keywords ? { keywords: content.seo.keywords } : {}),
  });
}

export default async function BlogArticlePage({ params }: Props) {
  const { slug } = await params;
  const content = getKnowledgeDetail(slug);
  if (!content || content.item.kind !== 'article') notFound();

  return (
    <>
      <JsonLdScript
        id={`blog-${slug}`}
        data={[
          ...buildPageSchemas({
            title: content.seo.title,
            description: content.seo.description,
            path: content.item.href,
            breadcrumbs: [
              { label: 'Home', href: '/' },
              { label: 'Resources', href: '/resources' },
              { label: 'Blog', href: '/resources/blog' },
              { label: content.item.title, href: content.item.href, current: true },
            ],
          }),
          buildArticleJsonLd(content.item),
        ]}
      />
      <ArticleDetailTemplate content={content} sectionKinds={['article']} />
    </>
  );
}
