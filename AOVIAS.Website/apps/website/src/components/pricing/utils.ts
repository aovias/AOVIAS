import type {
  Currency,
  PlanPrice,
  PricingPlan,
} from '@/components/pricing/models';

/**
 * Resolve a plan price for a billing cycle. Pricing logic stays out of UI.
 */
export function getPlanPrice(
  plan: PricingPlan,
  cycleId: string,
): PlanPrice | undefined {
  return plan.prices.find((price) => price.cycleId === cycleId);
}

export function formatPlanPrice(
  price: PlanPrice | undefined,
  currencies: readonly Currency[],
  options?: { fallback?: string },
): string {
  if (!price) return options?.fallback ?? '—';
  if (price.displayOverride) return price.displayOverride;
  if (price.amount === null) return options?.fallback ?? 'Contact us to get quote.';

  const currency =
    currencies.find((item) => item.code === price.currencyCode) ??
    ({
      code: price.currencyCode,
      symbol: price.currencyCode,
      locale: 'en-US',
    } satisfies Currency);

  try {
    return new Intl.NumberFormat(currency.locale ?? 'en-US', {
      style: 'currency',
      currency: currency.code,
      maximumFractionDigits: price.amount % 1 === 0 ? 0 : 2,
    }).format(price.amount);
  } catch {
    return `${currency.symbol}${price.amount}`;
  }
}

export function plansForProduct(
  plans: readonly PricingPlan[],
  planIds: readonly string[],
): PricingPlan[] {
  const map = new Map(plans.map((plan) => [plan.id, plan]));
  return planIds
    .map((id) => map.get(id))
    .filter((plan): plan is PricingPlan => Boolean(plan));
}
