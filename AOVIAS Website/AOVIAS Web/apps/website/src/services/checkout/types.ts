import type { BillingCycle, Currency, PricingPlan, Product } from '@/components/pricing';

/**
 * Checkout domain models — UI and adapters consume these shapes.
 * Payment processing and order fulfillment remain out of scope.
 */

export type Customer = {
  id?: string;
  name: string;
  email: string;
};

export type Organization = {
  name: string;
  taxIdentifier?: string;
  country: string;
};

export type BillingAddress = {
  line1: string;
  line2?: string;
  city: string;
  region?: string;
  postalCode: string;
  country: string;
};

export type BillingProfile = {
  customer: Customer;
  organization: Organization;
  address: BillingAddress;
  currencyCode: string;
  purchaseType: 'individual' | 'business' | 'enterprise-request';
};

export type CartLine = {
  id: string;
  product: Product;
  plan: PricingPlan;
  cycle: BillingCycle;
  quantity: number;
  unitAmount: number | null;
  currencyCode: string;
  displayPrice: string;
};

export type Cart = {
  id: string;
  lines: readonly CartLine[];
  currency: Currency;
  /** Placeholder until tax engine exists. */
  taxDisplay: string;
  /** Placeholder until discount engine exists. */
  discountDisplay: string;
  subtotalDisplay: string;
  totalDisplay: string;
  /** Enterprise / contact pricing path. */
  requiresSales?: boolean;
};

export type InvoicePlaceholder = {
  id: string;
  status: 'pending' | 'unavailable';
  message: string;
};

export type Order = {
  id: string;
  cart: Cart;
  billing: BillingProfile;
  status: 'draft' | 'submitted' | 'awaiting-payment' | 'completed';
  createdAt: string;
  invoice: InvoicePlaceholder;
  /** Future payment provider session id. */
  paymentSessionId?: string;
};

export type PurchaseSummary = {
  orderId: string;
  productName: string;
  planName: string;
  cycleLabel: string;
  quantity: number;
  totalDisplay: string;
  purchaseType: BillingProfile['purchaseType'];
  nextSteps: readonly string[];
};

export type CheckoutResult = {
  ok: boolean;
  order?: Order;
  summary?: PurchaseSummary;
  message?: string;
  retryable?: boolean;
};
