import { JsonLdScript } from '@/components/seo';
import { CompanyPage, companySeo } from '@/features/company';
import { companyFaqItems } from '@/features/company/faq/content';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';
import { ogImages } from '@/seo';

export const metadata = createPageMetadata({
  title: companySeo.title,
  description: companySeo.description,
  path: companySeo.path,
  type: 'marketing',
  keywords: companySeo.keywords,
  image: ogImages.company,
  imageAlt: 'AOVIAS Company',
});

/**
 * AOVIAS Company — institutional identity page.
 * Layout: MarketingLayout → AppShell (header/footer).
 */
export default function CompanyRoutePage() {
  return (
    <>
      <JsonLdScript
        id="company-webpage"
        data={buildPageSchemas({
          title: companySeo.title,
          description: companySeo.description,
          path: companySeo.path,
          pageType: 'AboutPage',
          includeOrganization: true,
          breadcrumbs: [
            { label: 'Home', href: '/' },
            { label: 'Company', href: companySeo.path, current: true },
          ],
          faq: companyFaqItems.map((item) => ({
            question: item.question,
            answer: item.answer,
          })),
        })}
      />
      <CompanyPage />
    </>
  );
}
