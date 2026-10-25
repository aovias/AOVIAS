import { JsonLdScript } from '@/components/seo';
import {
  buildContactPointSchemas,
  ContactPage,
  contactSeo,
} from '@/features/contact';
import { contactFaqItems } from '@/features/contact/faq/content';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';
import { ogImages } from '@/seo';

export const metadata = createPageMetadata({
  title: contactSeo.title,
  description: contactSeo.description,
  path: contactSeo.path,
  type: 'marketing',
  keywords: contactSeo.keywords,
  image: ogImages.contact,
  imageAlt: 'Contact AOVIAS',
});

/**
 * AOVIAS Contact & Enterprise Engagement.
 * Layout: MarketingLayout → AppShell (via (company) route group).
 */
export default function ContactRoutePage() {
  return (
    <>
      <JsonLdScript
        id="contact-webpage"
        data={buildPageSchemas({
          title: contactSeo.title,
          description: contactSeo.description,
          path: contactSeo.path,
          pageType: 'ContactPage',
          includeOrganization: true,
          breadcrumbs: [
            { label: 'Home', href: '/' },
            { label: 'Company', href: '/company' },
            { label: 'Contact', href: contactSeo.path, current: true },
          ],
          faq: contactFaqItems.map((item) => ({
            question: item.question,
            answer: item.answer,
          })),
        })}
      />
      <JsonLdScript
        id="contact-points"
        data={buildContactPointSchemas()}
      />
      <ContactPage />
    </>
  );
}
