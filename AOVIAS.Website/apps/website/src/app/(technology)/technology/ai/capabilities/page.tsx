import { JsonLdScript } from '@/components/seo';
import {
  AiCapabilitiesPage,
  aiCapabilitiesBreadcrumbs,
  aiPagesSeo,
} from '@/features/technology';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';

const seo = aiPagesSeo.capabilities;

export const metadata = createPageMetadata({
  title: seo.title,
  description: seo.description,
  path: seo.path,
  type: 'website',
  keywords: seo.keywords,
  image: '/og/technology.png',
  imageAlt: 'AOVIAS AI Capabilities',
});

export default function TechnologyAiCapabilitiesRoute() {
  return (
    <>
      <JsonLdScript
        id="technology-ai-capabilities"
        data={[
          ...buildPageSchemas({
            title: seo.title,
            description: seo.description,
            path: seo.path,
            includeOrganization: true,
            breadcrumbs: aiCapabilitiesBreadcrumbs(),
          }),
        ]}
      />
      <AiCapabilitiesPage />
    </>
  );
}
