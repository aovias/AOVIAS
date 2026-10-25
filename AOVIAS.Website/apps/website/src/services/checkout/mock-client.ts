import { buildCartFromSelection } from '@/services/checkout/cart-builder';
import type { CheckoutClient } from '@/services/checkout/client';
import type { CheckoutResult, Order, PurchaseSummary } from '@/services/checkout/types';

const orderStore = new Map<string, Order>();

async function delay(ms = 500) {
  await new Promise((resolve) => {
    setTimeout(resolve, ms);
  });
}

function createOrderId() {
  return `ORD-${Date.now().toString(36).toUpperCase()}`;
}

/**
 * Local mock Commerce service — no payment processing.
 */
export function createMockCheckoutClient(): CheckoutClient {
  return {
    buildCart(input) {
      return buildCartFromSelection(input);
    },

    async submitOrder({ cart, billing, acceptedTerms, acceptedPrivacy }) {
      await delay();

      if (!acceptedTerms || !acceptedPrivacy) {
        return {
          ok: false,
          message: 'Accept the terms and privacy acknowledgement to continue.',
          retryable: true,
        } satisfies CheckoutResult;
      }

      if (cart.requiresSales) {
        return {
          ok: false,
          message:
            'This selection requires Enterprise Sales. Submit an enterprise inquiry instead of self-serve checkout.',
          retryable: false,
        };
      }

      const orderId = createOrderId();
      const line = cart.lines[0];
      if (!line) {
        return {
          ok: false,
          message: 'Your cart is empty.',
          retryable: false,
        };
      }

      const order: Order = {
        id: orderId,
        cart,
        billing,
        status: 'awaiting-payment',
        createdAt: new Date().toISOString(),
        invoice: {
          id: `INV-PLACEHOLDER-${orderId}`,
          status: 'unavailable',
          message:
            'Invoice download will be available when billing infrastructure is connected.',
        },
      };

      orderStore.set(orderId, order);

      const summary: PurchaseSummary = {
        orderId,
        productName: line.product.name,
        planName: line.plan.name,
        cycleLabel: line.cycle.label,
        quantity: line.quantity,
        totalDisplay: cart.totalDisplay,
        purchaseType: billing.purchaseType,
        nextSteps: [
          'Payment provider integration will complete this purchase.',
          'You will receive access instructions after payment confirmation.',
          'Contact support if your organization needs a purchase order.',
        ],
      };

      return {
        ok: true,
        order,
        summary,
        message: 'Order recorded. Payment collection is not enabled in this environment.',
      };
    },

    async getOrder(orderId) {
      await delay(120);
      return orderStore.get(orderId) ?? null;
    },
  };
}

export const checkoutClient: CheckoutClient = createMockCheckoutClient();
