import { notFound } from 'next/navigation';

import { JsonLdScript } from '@/components/seo';
import { ProductDetailTemplate } from '@/features/products/detail/product-detail-template';
import {
  getAllProductDetailSlugs,
  getProductDetail,
} from '@/features/products/detail/registry';
import { buildProductSoftwareSchema } from '@/features/products/seo';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';

type ProductDetailPageProps = {
  params: Promise<{ slug: string }>;
};

export function generateStaticParams() {
  return getAllProductDetailSlugs().map((slug) => ({ slug }));
}

export async function generateMetadata({ params }: ProductDetailPageProps) {
  const { slug } = await params;
  const content = getProductDetail(slug);
  if (!content) {
    return {};
  }

  return createPageMetadata({
    title: content.seo.title,
    description: content.seo.description,
    path: `/products/${content.slug}`,
    type: 'product',
    ...(content.seo.keywords ? { keywords: content.seo.keywords } : {}),
    ...(content.status === 'planned' ? { robots: { index: true, follow: true } } : {}),
    image:
      content.slug === 'command'
        ? '/og/command.png'
        : content.slug === 'studio'
          ? '/og/studio.png'
          : content.slug === 'platform'
            ? '/og/products.png'
            : '/og/products.png',
    imageAlt: content.name,
  });
}

/**
 * Product detail route — configuration-driven via Product Detail Registry.
 * Layout: ProductLayout → AppShell.
 */
export default async function ProductDetailRoutePage({
  params,
}: ProductDetailPageProps) {
  const { slug } = await params;
  const content = getProductDetail(slug);

  if (!content) {
    notFound();
  }

  const path = `/products/${content.slug}`;

  return (
    <>
      <JsonLdScript
        id={`product-${content.slug}`}
        data={[
          ...buildPageSchemas({
            title: content.seo.title,
            description: content.seo.description,
            path,
            includeOrganization: true,
            breadcrumbs: [
              { label: 'Home', href: '/' },
              { label: 'Products', href: '/products' },
              { label: content.name, href: path, current: true },
            ],
          }),
          buildProductSoftwareSchema(content),
        ]}
      />
      <ProductDetailTemplate content={content} />
    </>
  );
}
