import { JsonLdScript } from '@/components/seo';
import {
  buildPricingOfferSchemas,
  PricingPage,
  pricingSeo,
} from '@/features/pricing';
import { pricingFaqItems } from '@/features/pricing/domain';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';
import { ogImages } from '@/seo';

export const metadata = createPageMetadata({
  title: pricingSeo.title,
  description: pricingSeo.description,
  path: pricingSeo.path,
  type: 'product',
  keywords: pricingSeo.keywords,
  image: ogImages.pricing,
  imageAlt: 'AOVIAS Pricing',
});

/**
 * AOVIAS Pricing — commercial gateway for Studio and Command.
 * Layout: ProductLayout → AppShell (header/footer/breadcrumbs).
 */
export default function PricingRoutePage() {
  return (
    <>
      <JsonLdScript
        id="pricing-webpage"
        data={[
          ...buildPageSchemas({
            title: pricingSeo.title,
            description: pricingSeo.description,
            path: pricingSeo.path,
            includeOrganization: true,
            breadcrumbs: [
              { label: 'Home', href: '/' },
              { label: 'Products', href: '/products' },
              { label: 'Pricing', href: pricingSeo.path, current: true },
            ],
            faq: pricingFaqItems.map((item) => ({
              question: item.question,
              answer: item.answer,
            })),
          }),
          ...buildPricingOfferSchemas(),
        ]}
      />
      <PricingPage />
    </>
  );
}
