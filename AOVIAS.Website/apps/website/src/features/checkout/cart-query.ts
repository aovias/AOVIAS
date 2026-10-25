import { buildCartFromSelection } from '@/services/checkout';

export type CheckoutSelection = {
  productId: string;
  planId: string;
  cycleId: string;
  quantity: number;
};

export function parseCheckoutSelection(input: {
  productId?: string;
  planId?: string;
  cycleId?: string;
  quantity?: string;
}): CheckoutSelection {
  return {
    productId: input.productId?.trim() || 'studio',
    planId: input.planId?.trim() || 'studio-professional',
    cycleId: input.cycleId?.trim() || 'annual',
    quantity: Math.max(1, Math.min(99, Number(input.quantity ?? '1') || 1)),
  };
}

export function selectionToQuery(selection: CheckoutSelection): string {
  const params = new URLSearchParams({
    productId: selection.productId,
    planId: selection.planId,
    cycleId: selection.cycleId,
    quantity: String(selection.quantity),
  });
  return params.toString();
}

export function resolveCart(selection: CheckoutSelection) {
  return buildCartFromSelection(selection);
}
