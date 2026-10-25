import { JsonLdScript } from '@/components/seo';
import {
  WhyTechnologyPage,
  ecosystemPagesSeo,
  whyTechnologyBreadcrumbs,
} from '@/features/technology';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';

const seo = ecosystemPagesSeo.why;

export const metadata = createPageMetadata({
  title: seo.title,
  description: seo.description,
  path: seo.path,
  type: 'website',
  keywords: seo.keywords,
  image: '/og/technology.png',
  imageAlt: 'Why AOVIAS Technology',
});

export default function TechnologyWhyRoute() {
  return (
    <>
      <JsonLdScript
        id="technology-why"
        data={[
          ...buildPageSchemas({
            title: seo.title,
            description: seo.description,
            path: seo.path,
            includeOrganization: true,
            breadcrumbs: whyTechnologyBreadcrumbs(),
          }),
        ]}
      />
      <WhyTechnologyPage />
    </>
  );
}