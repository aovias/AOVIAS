import type { PricingPlan } from '@/components/pricing';
import { formatPlanPrice, getPlanPrice } from '@/components/pricing';
import { seoConfig } from '@/config/seo';
import {
  pricingCurrencies,
  pricingPlans,
  pricingProducts,
} from '@/features/pricing/domain';
import { buildCanonicalUrl } from '@/lib/seo/canonical';
import type { JsonLd } from '@/lib/seo/schema';

export const pricingSeo = {
  title: 'Pricing — Transparent Institutional Licensing',
  description:
    'Published AOVIAS pricing for Studio and Command, plus enterprise engagement for institutional licensing, implementation, and support.',
  path: '/pricing',
  keywords: [
    'AOVIAS pricing',
    'AOVIAS Studio pricing',
    'AOVIAS Command pricing',
    'enterprise licensing',
    'institutional software pricing',
  ],
} as const;

/**
 * Product + Offer schema for commercially listed plans (amount present).
 */
export function buildPricingOfferSchemas(
  plans: readonly PricingPlan[] = pricingPlans,
): JsonLd[] {
  const schemas: JsonLd[] = [];

  for (const product of pricingProducts) {
    const productPlans = product.planIds
      .map((id) => plans.find((plan) => plan.id === id))
      .filter((plan): plan is PricingPlan => Boolean(plan));

    for (const plan of productPlans) {
      const annual = getPlanPrice(plan, 'annual');
      const monthly = getPlanPrice(plan, 'monthly');
      const price = annual?.amount != null ? annual : monthly;
      if (!price || price.amount == null) continue;

      schemas.push({
        '@context': 'https://schema.org',
        '@type': 'Product',
        name: `${product.name} — ${plan.name}`,
        description: plan.description ?? product.description,
        brand: {
          '@type': 'Brand',
          name: seoConfig.organization.name,
        },
        offers: {
          '@type': 'Offer',
          price: price.amount,
          priceCurrency: price.currencyCode,
          availability: 'https://schema.org/InStock',
          url: buildCanonicalUrl(pricingSeo.path),
          description: formatPlanPrice(price, pricingCurrencies),
        },
      });
    }
  }

  return schemas;
}
