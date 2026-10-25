import { JsonLdScript } from '@/components/seo';
import {
  EngineeringDesignPrinciplesPage,
  engineeringDesignPrinciplesBreadcrumbs,
  engineeringPagesSeo,
} from '@/features/technology';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';

const seo = engineeringPagesSeo.designPrinciples;

export const metadata = createPageMetadata({
  title: seo.title,
  description: seo.description,
  path: seo.path,
  type: 'website',
  keywords: seo.keywords,
  image: '/og/technology.png',
  imageAlt: 'AOVIAS Design Principles',
});

export default function TechnologyEngineeringDesignPrinciplesRoute() {
  return (
    <>
      <JsonLdScript
        id="technology-engineering-design-principles"
        data={[
          ...buildPageSchemas({
            title: seo.title,
            description: seo.description,
            path: seo.path,
            includeOrganization: true,
            breadcrumbs: engineeringDesignPrinciplesBreadcrumbs(),
          }),
        ]}
      />
      <EngineeringDesignPrinciplesPage />
    </>
  );
}
