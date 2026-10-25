import {
  createSdkError,
  paginateArray,
  sdkFail,
  sdkOk,
  type CommerceCart,
  type CommerceClient,
  type CommercePlanSummary,
  type CommerceProductSummary,
} from '@aovias/sdk';

import {
  pricingBillingCycles,
  pricingPlans,
  pricingProducts,
} from '@/features/pricing/domain/catalog';
import { mockDelay } from '@/mocks/delay';
import { buildCartFromSelection } from '@/services/checkout/cart-builder';

function toProductSummary(
  product: (typeof pricingProducts)[number],
): CommerceProductSummary {
  return {
    id: product.id,
    name: product.name,
    slug: product.id,
    ...(product.description ? { description: product.description } : {}),
  };
}

function toPlanSummaries(): CommercePlanSummary[] {
  const cycles = new Map(pricingBillingCycles.map((c) => [c.id, c]));
  const summaries: CommercePlanSummary[] = [];
  for (const plan of pricingPlans) {
    const product = pricingProducts.find((p) => p.planIds.includes(plan.id));
    if (!product) continue;
    for (const price of plan.prices) {
      const cycle = cycles.get(price.cycleId);
      summaries.push({
        id: `${plan.id}:${price.cycleId}`,
        productId: product.id,
        name: plan.name,
        unitAmount: price.amount,
        currencyCode: price.currencyCode,
        cycleId: price.cycleId,
        cycleLabel: cycle?.label ?? price.cycleId,
        featured: Boolean(plan.badge),
        ...(plan.description ? { description: plan.description } : {}),
      });
    }
  }
  return summaries;
}

function mapCart(cart: NonNullable<ReturnType<typeof buildCartFromSelection>>): CommerceCart {
  return {
    id: cart.id,
    currencyCode: cart.currency.code,
    subtotalDisplay: cart.subtotalDisplay,
    totalDisplay: cart.totalDisplay,
    taxDisplay: cart.taxDisplay,
    discountDisplay: cart.discountDisplay,
    ...(cart.requiresSales !== undefined
      ? { requiresSales: cart.requiresSales }
      : {}),
    lines: cart.lines.map((line) => ({
      id: line.id,
      productId: line.product.id,
      productName: line.product.name,
      planId: line.plan.id,
      planName: line.plan.name,
      cycleId: line.cycle.id,
      cycleLabel: line.cycle.label,
      quantity: line.quantity,
      unitAmount: line.unitAmount,
      currencyCode: line.currencyCode,
      displayPrice: line.displayPrice,
    })),
  };
}

/**
 * Mock Commerce SDK — static pricing catalog; no payment providers.
 */
export function createMockCommerceClient(): CommerceClient {
  return {
    async listProducts() {
      await mockDelay(200);
      return sdkOk(pricingProducts.map(toProductSummary));
    },

    async getProduct(productId) {
      await mockDelay(150);
      const product = pricingProducts.find((p) => p.id === productId);
      return sdkOk(product ? toProductSummary(product) : null);
    },

    async listPlans(input) {
      await mockDelay(200);
      let plans = toPlanSummaries();
      if (input?.productId) {
        plans = plans.filter((p) => p.productId === input.productId);
      }
      return sdkOk(plans);
    },

    async getPlan(planId) {
      await mockDelay(150);
      const plan = toPlanSummaries().find((p) => p.id === planId);
      return sdkOk(plan ?? null);
    },

    async buildCart(input) {
      await mockDelay(200);
      const cart = buildCartFromSelection(input);
      if (!cart) {
        return sdkFail(
          createSdkError('not_found', 'Unable to build cart for that selection.'),
        );
      }
      return sdkOk(mapCart(cart));
    },

    async submitCheckout({ acceptedTerms, acceptedPrivacy, cart }) {
      await mockDelay();
      if (!acceptedTerms || !acceptedPrivacy) {
        return {
          ok: false,
          message: 'Accept the terms and privacy acknowledgement to continue.',
          retryable: true,
        };
      }
      if (cart.requiresSales) {
        return {
          ok: false,
          message:
            'This selection requires Enterprise Sales. Submit an enterprise inquiry instead of self-serve checkout.',
          retryable: false,
        };
      }
      return {
        ok: true,
        message: 'Order submitted (mock).',
        order: {
          id: `ORD-${Date.now().toString(36).toUpperCase()}`,
          cart,
          billing: {
            customerName: 'Mock Customer',
            customerEmail: 'mock@aovias.com',
            organizationName: 'Mock Org',
            country: 'US',
            currencyCode: cart.currencyCode,
            purchaseType: 'business',
          },
          status: 'submitted',
          createdAt: new Date().toISOString(),
        },
        redirectTo: '/checkout/success',
      };
    },

    async getOrder() {
      await mockDelay(150);
      return sdkOk(null);
    },

    async listSubscriptions() {
      await mockDelay(150);
      return sdkOk([]);
    },

    getClientInfo() {
      return { name: 'commerce', mode: 'mock', version: '0.1.0' };
    },
  };
}

/** Convenience for callers that need pagination over plans. */
export function paginateCommercePlans(
  plans: readonly CommercePlanSummary[],
  page = 1,
  pageSize = 20,
) {
  return paginateArray(plans, { page, pageSize });
}
