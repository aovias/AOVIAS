import type {
  BillingProfile,
  Cart,
  CheckoutResult,
  Order,
} from '@/services/checkout/types';

/**
 * Commerce checkout client — payment-provider independent.
 * Replace mock with Commerce API adapters without changing page UIs.
 */
export type CheckoutClient = {
  buildCart: (input: {
    productId: string;
    planId: string;
    cycleId: string;
    quantity?: number;
  }) => Cart | null;

  submitOrder: (input: {
    cart: Cart;
    billing: BillingProfile;
    acceptedTerms: boolean;
    acceptedPrivacy: boolean;
    /** CSRF-ready slot for future Commerce API. */
    csrfToken?: string;
  }) => Promise<CheckoutResult>;

  getOrder: (orderId: string) => Promise<Order | null>;
};
