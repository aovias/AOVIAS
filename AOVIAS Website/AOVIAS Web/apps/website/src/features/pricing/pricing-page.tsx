import {
  CommerceEditorialTemplate,
  pricingCommerceContent,
} from '@/features/commerce/editorial';
import { PricingCommerceBand } from '@/features/pricing/commerce/pricing-commerce';
import { PricingFaqSection } from '@/features/pricing/faq/section';

/**
 * Pricing — Enterprise Procurement Experience (Phase V9).
 */
export function PricingPage() {
  return (
    <CommerceEditorialTemplate
      content={pricingCommerceContent}
      commerceBand={<PricingCommerceBand />}
      afterJourney={<PricingFaqSection />}
    />
  );
}
