import { PricingCard } from '@/components/pricing';
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Stack,
  Text,
} from '@/components/primitives';
import { pricingCurrencies } from '@/features/pricing';
import type { Cart } from '@/services/checkout';

export type OrderSummaryProps = {
  cart: Cart;
};

/**
 * Order Summary — composed from Commerce PricingCard + totals (no new primitives).
 */
export function OrderSummary({ cart }: OrderSummaryProps) {
  const line = cart.lines[0];
  if (!line) return null;

  return (
    <Stack gap={6} className="w-full">
      <Stack gap={2}>
        <Text as="p" size="caption" weight="medium" tone="muted">
          {line.product.name}
        </Text>
        <PricingCard
          plan={line.plan}
          cycle={line.cycle}
          currencies={pricingCurrencies}
        />
      </Stack>

      <Card variant="elevated" className="bg-card">
        <CardHeader>
          <CardTitle>Commercial summary</CardTitle>
        </CardHeader>
        <CardContent>
          <dl className="flex flex-col gap-3">
            <div className="flex justify-between gap-4">
              <dt className="text-muted-foreground text-small">Quantity</dt>
              <dd className="text-foreground text-small font-medium">
                {line.quantity}
              </dd>
            </div>
            <div className="flex justify-between gap-4">
              <dt className="text-muted-foreground text-small">Unit price</dt>
              <dd className="text-foreground text-small font-medium">
                {line.displayPrice}
              </dd>
            </div>
            <div className="flex justify-between gap-4">
              <dt className="text-muted-foreground text-small">Subtotal</dt>
              <dd className="text-foreground text-small font-medium">
                {cart.subtotalDisplay}
              </dd>
            </div>
            <div className="flex justify-between gap-4">
              <dt className="text-muted-foreground text-small">Taxes</dt>
              <dd className="text-foreground text-small">{cart.taxDisplay}</dd>
            </div>
            <div className="flex justify-between gap-4">
              <dt className="text-muted-foreground text-small">Discounts</dt>
              <dd className="text-foreground text-small">
                {cart.discountDisplay}
              </dd>
            </div>
            <div className="border-border flex justify-between gap-4 border-t pt-3">
              <dt className="text-foreground text-small font-semibold">Total</dt>
              <dd className="text-foreground text-small font-semibold">
                {cart.totalDisplay}
              </dd>
            </div>
          </dl>
        </CardContent>
      </Card>
    </Stack>
  );
}
