import type { WithSdkInfo } from '../core/client';
import type { SdkResult } from '../core/result';
import type {
  CommerceCart,
  CommerceCheckoutResult,
  CommerceOrder,
  CommercePlanSummary,
  CommerceProductSummary,
  CommerceSubscriptionSummary,
  CommerceBillingProfile,
} from './types';

/**
 * Commerce SDK — pricing, cart, checkout, subscriptions.
 * No payment providers in R1.5 mocks.
 */
export type CommerceClient = WithSdkInfo & {
  listProducts: () => Promise<SdkResult<readonly CommerceProductSummary[]>>;

  getProduct: (
    productId: string,
  ) => Promise<SdkResult<CommerceProductSummary | null>>;

  listPlans: (input?: {
    productId?: string;
  }) => Promise<SdkResult<readonly CommercePlanSummary[]>>;

  getPlan: (planId: string) => Promise<SdkResult<CommercePlanSummary | null>>;

  buildCart: (input: {
    productId: string;
    planId: string;
    cycleId: string;
    quantity?: number;
  }) => Promise<SdkResult<CommerceCart | null>>;

  submitCheckout: (input: {
    cart: CommerceCart;
    billing: CommerceBillingProfile;
    acceptedTerms: boolean;
    acceptedPrivacy: boolean;
    csrfToken?: string;
  }) => Promise<CommerceCheckoutResult>;

  getOrder: (orderId: string) => Promise<SdkResult<CommerceOrder | null>>;

  listSubscriptions: () => Promise<
    SdkResult<readonly CommerceSubscriptionSummary[]>
  >;
};
