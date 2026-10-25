export { buildCartFromSelection } from '@/services/checkout/cart-builder';
export type { CheckoutClient } from '@/services/checkout/client';
export {
  billingValuesToProfile,
  createBillingSubmitHandler,
  createConfirmSubmitHandler,
  loadBillingProfile,
  loadLastOrderId,
  saveBillingProfile,
  saveLastOrderId,
  type BillingFormValues,
  type ConfirmFormValues,
} from '@/services/checkout/form-adapters';
export { createMockCheckoutClient } from '@/services/checkout/mock-client';
export { getCheckoutClient } from '@/registries/sdk-registry';
export type {
  BillingAddress,
  BillingProfile,
  Cart,
  CartLine,
  CheckoutResult,
  Customer,
  InvoicePlaceholder,
  Order,
  Organization,
  PurchaseSummary,
} from '@/services/checkout/types';
