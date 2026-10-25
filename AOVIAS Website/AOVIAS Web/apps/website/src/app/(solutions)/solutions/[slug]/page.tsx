import { notFound } from 'next/navigation';

import { JsonLdScript } from '@/components/seo';
import { IndustryTemplate } from '@/features/industries/detail/industry-template';
import {
  getAllIndustryDetailSlugs,
  getIndustryDetail,
} from '@/features/industries/detail/registry';
import {
  getIndustryEditorial,
} from '@/features/industries/editorial';
import { IndustryEditorialTemplate } from '@/features/industries/editorial';
import { buildIndustryPageSchema } from '@/features/industries/seo';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';

type IndustryDetailPageProps = {
  params: Promise<{ slug: string }>;
};

export function generateStaticParams() {
  return getAllIndustryDetailSlugs().map((slug) => ({ slug }));
}

export async function generateMetadata({ params }: IndustryDetailPageProps) {
  const { slug } = await params;
  const editorial = getIndustryEditorial(slug);
  if (editorial) {
    return createPageMetadata({
      title: editorial.seo.title,
      description: editorial.seo.description,
      path: `/solutions/${editorial.slug}`,
      type: 'marketing',
      ...(editorial.seo.keywords ? { keywords: editorial.seo.keywords } : {}),
    });
  }

  const content = getIndustryDetail(slug);
  if (!content) {
    return {};
  }

  return createPageMetadata({
    title: content.seo.title,
    description: content.seo.description,
    path: `/solutions/${content.slug}`,
    type: 'marketing',
    ...(content.seo.keywords ? { keywords: content.seo.keywords } : {}),
  });
}

/**
 * Industry detail route — V6 editorial hubs or classic IndustryTemplate.
 */
export default async function IndustryDetailRoutePage({
  params,
}: IndustryDetailPageProps) {
  const { slug } = await params;
  const editorial = getIndustryEditorial(slug);

  if (editorial) {
    const path = `/solutions/${editorial.slug}`;
    return (
      <>
        <JsonLdScript
          id={`industry-${editorial.slug}`}
          data={[
            ...buildPageSchemas({
              title: editorial.seo.title,
              description: editorial.seo.description,
              path,
              includeOrganization: true,
              breadcrumbs: [
                { label: 'Home', href: '/' },
                { label: 'Solutions', href: '/solutions' },
                { label: editorial.name, href: path, current: true },
              ],
            }),
            buildIndustryPageSchema({
              slug: editorial.slug,
              name: editorial.name,
              seo: editorial.seo,
              overviewDescription: editorial.reality.description,
            }),
          ]}
        />
        <IndustryEditorialTemplate content={editorial} />
      </>
    );
  }

  const content = getIndustryDetail(slug);

  if (!content) {
    notFound();
  }

  const path = `/solutions/${content.slug}`;

  return (
    <>
      <JsonLdScript
        id={`industry-${content.slug}`}
        data={[
          ...buildPageSchemas({
            title: content.seo.title,
            description: content.seo.description,
            path,
            includeOrganization: true,
            breadcrumbs: [
              { label: 'Home', href: '/' },
              { label: 'Solutions', href: '/solutions' },
              { label: content.name, href: path, current: true },
            ],
          }),
          buildIndustryPageSchema({
            slug: content.slug,
            name: content.name,
            seo: content.seo,
            overviewDescription: content.overview.description,
          }),
        ]}
      />
      <IndustryTemplate content={content} />
    </>
  );
}
