import { notFound } from 'next/navigation';

import { JsonLdScript } from '@/components/seo';
import {
  PlatformArchitecturePage,
  TechnologyTopicPlaceholderPage,
  getTechnologyTopicBySlug,
  isTechnologyLiveSlug,
  platformArchitectureBreadcrumbs,
  platformPagesSeo,
  technologyRouteSlugs,
  technologyTopicBreadcrumbs,
  technologyTopicSeo,
} from '@/features/technology';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';

type TechnologyTopicRouteProps = {
  params: Promise<{ slug: string }>;
};

export function generateStaticParams() {
  return technologyRouteSlugs.map((slug) => ({ slug }));
}

export async function generateMetadata({ params }: TechnologyTopicRouteProps) {
  const { slug } = await params;
  if (slug === 'architecture') {
    const seo = platformPagesSeo.architecture;
    return createPageMetadata({
      title: seo.title,
      description: seo.description,
      path: seo.path,
      type: 'website',
      keywords: seo.keywords,
      image: '/og/technology.png',
      imageAlt: 'AOVIAS Platform Architecture',
    });
  }

  const topic = getTechnologyTopicBySlug(slug);
  if (!topic) {
    return {};
  }
  const seo = technologyTopicSeo(slug);
  return createPageMetadata({
    title: seo.title,
    description: seo.description,
    path: seo.path,
    type: 'website',
    keywords: seo.keywords,
    image: '/og/technology.png',
    imageAlt: `AOVIAS Technology — ${topic.label}`,
  });
}

/**
 * Technology child topic — live pages or Coming Soon placeholders.
 */
export default async function TechnologyTopicRoutePage({
  params,
}: TechnologyTopicRouteProps) {
  const { slug } = await params;
  if (!getTechnologyTopicBySlug(slug)) {
    notFound();
  }

  if (isTechnologyLiveSlug(slug) && slug === 'architecture') {
    const seo = platformPagesSeo.architecture;
    return (
      <>
        <JsonLdScript
          id="technology-architecture"
          data={[
            ...buildPageSchemas({
              title: seo.title,
              description: seo.description,
              path: seo.path,
              includeOrganization: true,
              breadcrumbs: platformArchitectureBreadcrumbs(),
            }),
          ]}
        />
        <PlatformArchitecturePage />
      </>
    );
  }

  const seo = technologyTopicSeo(slug);

  return (
    <>
      <JsonLdScript
        id={`technology-${slug}-webpage`}
        data={[
          ...buildPageSchemas({
            title: seo.title,
            description: seo.description,
            path: seo.path,
            includeOrganization: true,
            breadcrumbs: technologyTopicBreadcrumbs(slug),
          }),
        ]}
      />
      <TechnologyTopicPlaceholderPage slug={slug} />
    </>
  );
}
