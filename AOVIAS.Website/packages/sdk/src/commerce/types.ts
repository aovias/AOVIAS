/**
 * Portable commerce models — independent of UI component types.
 * Website adapters map catalog / checkout UI models to these shapes.
 */

export type CommerceCurrency = {
  code: string;
  symbol?: string;
};

export type CommerceProductSummary = {
  id: string;
  name: string;
  description?: string;
  slug?: string;
};

export type CommercePlanSummary = {
  id: string;
  productId: string;
  name: string;
  description?: string;
  /** Null = contact sales / custom. */
  unitAmount: number | null;
  currencyCode: string;
  cycleId: string;
  cycleLabel: string;
  featured?: boolean;
};

export type CommerceSubscriptionSummary = {
  id: string;
  productId: string;
  planId: string;
  status: 'active' | 'trialing' | 'canceled' | 'past_due' | 'none';
  renewsAt?: string;
};

export type CommerceBillingProfile = {
  customerName: string;
  customerEmail: string;
  organizationName: string;
  country: string;
  currencyCode: string;
  purchaseType: 'individual' | 'business' | 'enterprise-request';
};

export type CommerceCartLine = {
  id: string;
  productId: string;
  productName: string;
  planId: string;
  planName: string;
  cycleId: string;
  cycleLabel: string;
  quantity: number;
  unitAmount: number | null;
  currencyCode: string;
  displayPrice: string;
};

export type CommerceCart = {
  id: string;
  lines: readonly CommerceCartLine[];
  currencyCode: string;
  subtotalDisplay: string;
  totalDisplay: string;
  taxDisplay: string;
  discountDisplay: string;
  requiresSales?: boolean;
};

export type CommerceOrder = {
  id: string;
  cart: CommerceCart;
  billing: CommerceBillingProfile;
  status: 'draft' | 'submitted' | 'awaiting-payment' | 'completed';
  createdAt: string;
};

export type CommerceCheckoutResult = {
  ok: boolean;
  order?: CommerceOrder;
  message?: string;
  retryable?: boolean;
  redirectTo?: string;
};
