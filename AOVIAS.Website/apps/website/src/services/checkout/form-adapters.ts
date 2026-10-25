import type { FormSubmitHandler, FormSubmitResult } from '@/features/forms';
import { getCheckoutClient } from '@/registries/sdk-registry';
import type { BillingProfile, Cart } from '@/services/checkout/types';

const BILLING_STORAGE_KEY = 'aovias.checkout.billing';
const ORDER_STORAGE_KEY = 'aovias.checkout.lastOrderId';

export function saveBillingProfile(profile: BillingProfile) {
  if (typeof window === 'undefined') return;
  window.sessionStorage.setItem(BILLING_STORAGE_KEY, JSON.stringify(profile));
}

export function loadBillingProfile(): BillingProfile | null {
  if (typeof window === 'undefined') return null;
  const raw = window.sessionStorage.getItem(BILLING_STORAGE_KEY);
  if (!raw) return null;
  try {
    return JSON.parse(raw) as BillingProfile;
  } catch {
    return null;
  }
}

export function saveLastOrderId(orderId: string) {
  if (typeof window === 'undefined') return;
  window.sessionStorage.setItem(ORDER_STORAGE_KEY, orderId);
}

export function loadLastOrderId(): string | null {
  if (typeof window === 'undefined') return null;
  return window.sessionStorage.getItem(ORDER_STORAGE_KEY);
}

export type BillingFormValues = {
  purchaseType: 'individual' | 'business' | 'enterprise-request';
  contactName: string;
  email: string;
  organizationName: string;
  taxIdentifier: string;
  country: string;
  currencyCode: string;
  addressLine1: string;
  addressLine2: string;
  city: string;
  region: string;
  postalCode: string;
};

export function billingValuesToProfile(
  values: BillingFormValues,
): BillingProfile {
  return {
    purchaseType: values.purchaseType,
    customer: {
      name: values.contactName,
      email: values.email,
    },
    organization: {
      name: values.organizationName,
      ...(values.taxIdentifier.trim()
        ? { taxIdentifier: values.taxIdentifier.trim() }
        : {}),
      country: values.country,
    },
    address: {
      line1: values.addressLine1,
      ...(values.addressLine2.trim()
        ? { line2: values.addressLine2.trim() }
        : {}),
      city: values.city,
      ...(values.region.trim() ? { region: values.region.trim() } : {}),
      postalCode: values.postalCode,
      country: values.country,
    },
    currencyCode: values.currencyCode,
  };
}

/** Persist billing profile for confirm step — no backend call. */
export function createBillingSubmitHandler(): FormSubmitHandler<BillingFormValues> {
  return async (values) => {
    saveBillingProfile(billingValuesToProfile(values));
    return {
      ok: true,
      message: 'Billing details saved for review.',
      followUp: 'Continue to confirmation to place your order request.',
    } satisfies FormSubmitResult;
  };
}

export type ConfirmFormValues = {
  acceptedTerms: boolean;
  acceptedPrivacy: boolean;
};

export function createConfirmSubmitHandler(cart: Cart): FormSubmitHandler<ConfirmFormValues> {
  return async (values) => {
    const billing = loadBillingProfile();
    if (!billing) {
      return {
        ok: false,
        message: 'Billing information is missing. Return to the billing step.',
        retryable: false,
      };
    }

    const result = await getCheckoutClient().submitOrder({
      cart,
      billing,
      acceptedTerms: values.acceptedTerms,
      acceptedPrivacy: values.acceptedPrivacy,
    });

    if (!result.ok || !result.order) {
      return {
        ok: false,
        message: result.message ?? 'Unable to submit order.',
        ...(result.retryable !== undefined
          ? { retryable: result.retryable }
          : {}),
      };
    }

    saveLastOrderId(result.order.id);
    return {
      ok: true,
      message: result.message ?? 'Order submitted.',
      followUp: `Reference ${result.order.id}`,
    };
  };
}
