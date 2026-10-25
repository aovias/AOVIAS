import { notFound } from 'next/navigation';

import { JsonLdScript } from '@/components/seo';
import {
  getCategoryBySlug,
  getCategoryIntro,
  getKnowledgeByCategorySlug,
  knowledgeCategories,
  KnowledgeListPage,
} from '@/features/knowledge';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';

type Props = { params: Promise<{ slug: string }> };

export function generateStaticParams() {
  return knowledgeCategories.map((category) => ({
    slug: category.slug ?? category.id,
  }));
}

export async function generateMetadata({ params }: Props) {
  const { slug } = await params;
  const category = getCategoryBySlug(slug);
  if (!category) return {};
  return createPageMetadata({
    title: category.name,
    description: `AOVIAS knowledge in the ${category.name} category.`,
    path: `/resources/categories/${slug}`,
    type: 'marketing',
  });
}

export default async function CategoryDetailPage({ params }: Props) {
  const { slug } = await params;
  const category = getCategoryBySlug(slug);
  if (!category) notFound();

  const items = getKnowledgeByCategorySlug(slug);
  const path = `/resources/categories/${slug}`;

  return (
    <>
      <JsonLdScript
        id={`category-${slug}`}
        data={buildPageSchemas({
          title: category.name,
          description: `${category.name} knowledge`,
          path,
          breadcrumbs: [
            { label: 'Home', href: '/' },
            { label: 'Resources', href: '/resources' },
            { label: 'Categories', href: '/resources/categories' },
            { label: category.name, href: path, current: true },
          ],
        })}
      />
      <KnowledgeListPage
        eyebrow="Category"
        title={category.name}
        description={getCategoryIntro(slug, category.name)}
        items={items}
        categories={knowledgeCategories}
        activeCategorySlug={slug}
        emptyMessage="No content is assigned to this category yet."
      />
    </>
  );
}
