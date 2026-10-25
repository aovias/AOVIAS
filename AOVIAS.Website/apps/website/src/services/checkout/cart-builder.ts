import {
  formatPlanPrice,
  getPlanPrice,
  type BillingCycle,
  type PricingPlan,
  type Product,
} from '@/components/pricing';
import {
  pricingBillingCycles,
  pricingCurrencies,
  pricingPlans,
  pricingProducts,
} from '@/features/pricing';
import type { Cart, CartLine } from '@/services/checkout/types';

function findProduct(productId: string): Product | undefined {
  return pricingProducts.find((item) => item.id === productId);
}

function findPlan(planId: string): PricingPlan | undefined {
  return pricingPlans.find((item) => item.id === planId);
}

function findCycle(cycleId: string): BillingCycle | undefined {
  return pricingBillingCycles.find((item) => item.id === cycleId);
}

/**
 * Build a configuration-driven cart from the Pricing domain + Product ids.
 */
export function buildCartFromSelection(input: {
  productId: string;
  planId: string;
  cycleId: string;
  quantity?: number;
}): Cart | null {
  const product = findProduct(input.productId);
  const plan = findPlan(input.planId);
  const cycle = findCycle(input.cycleId);
  const currency = pricingCurrencies[0];

  if (!product || !plan || !cycle || !currency) return null;
  if (!product.planIds.includes(plan.id)) return null;

  const quantity = Math.max(1, Math.min(99, input.quantity ?? 1));
  const price = getPlanPrice(plan, cycle.id);
  const unitAmount = price?.amount ?? null;
  const displayPrice = formatPlanPrice(price, pricingCurrencies);
  const requiresSales =
    plan.variant === 'enterprise' || unitAmount === null;

  let subtotalDisplay = displayPrice;
  if (unitAmount != null && quantity > 1) {
    subtotalDisplay = formatPlanPrice(
      {
        cycleId: cycle.id,
        amount: unitAmount * quantity,
        currencyCode: price?.currencyCode ?? currency.code,
      },
      pricingCurrencies,
    );
  }

  const line: CartLine = {
    id: `${product.id}-${plan.id}-${cycle.id}`,
    product,
    plan,
    cycle,
    quantity,
    unitAmount,
    currencyCode: price?.currencyCode ?? currency.code,
    displayPrice,
  };

  return {
    id: `cart-${line.id}`,
    lines: [line],
    currency,
    taxDisplay: 'Calculated at payment',
    discountDisplay: 'None applied',
    subtotalDisplay,
    totalDisplay: requiresSales ? 'Custom — contact enterprise' : subtotalDisplay,
    requiresSales,
  };
}
