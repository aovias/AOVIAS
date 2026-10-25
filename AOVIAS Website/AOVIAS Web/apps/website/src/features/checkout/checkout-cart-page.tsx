'use client';

import { useMemo, useState } from 'react';
import { useRouter } from 'next/navigation';

import { Section } from '@/components/layout';
import { MarketingCtaLink, SectionHeader } from '@/components/marketing';
import {
  BillingToggle,
  EnterpriseCta,
  PlanSelector,
  plansForProduct,
} from '@/components/pricing';
import { Stack, Text } from '@/components/primitives';
import { CheckoutSteps } from '@/features/checkout/checkout-steps';
import {
  selectionToQuery,
  type CheckoutSelection,
} from '@/features/checkout/cart-query';
import { OrderSummary } from '@/features/checkout/order-summary';
import {
  enterpriseOffering,
  pricingBillingCycles,
  pricingPlans,
  pricingProducts,
} from '@/features/pricing';
import { buildCartFromSelection } from '@/services/checkout';

export type CheckoutCartPageProps = {
  initial: CheckoutSelection;
};

/**
 * Checkout entry — product/plan/cycle selection + order summary.
 */
export function CheckoutCartPage({ initial }: CheckoutCartPageProps) {
  const router = useRouter();
  const [productId, setProductId] = useState(initial.productId);
  const [planId, setPlanId] = useState(initial.planId);
  const [cycleId, setCycleId] = useState(initial.cycleId);
  const [quantity, setQuantity] = useState(initial.quantity);

  const selection = useMemo(
    () => ({ productId, planId, cycleId, quantity }),
    [productId, planId, cycleId, quantity],
  );
  const query = selectionToQuery(selection);
  const cart = useMemo(() => buildCartFromSelection(selection), [selection]);

  const productPlans = useMemo(
    () =>
      plansForProduct(
        pricingPlans,
        pricingProducts.find((item) => item.id === productId)?.planIds ?? [],
      ),
    [productId],
  );

  if (!cart) {
    return (
      <Section spacing="md">
        <SectionHeader
          eyebrow="Checkout"
          heading="Unable to build cart"
          description="Select a valid product and plan from Pricing, then return to checkout."
        />
        <MarketingCtaLink label="View pricing" href="/pricing" variant="primary" />
      </Section>
    );
  }

  return (
    <Section spacing="md" className="bg-muted/20 -mx-4 px-4 sm:-mx-6 sm:px-6 lg:-mx-8 lg:px-8">
      <Stack gap={8}>
        <CheckoutSteps current="cart" query={query} />
        <SectionHeader
          eyebrow="Procurement"
          heading="Review your commercial selection"
          description="Confirm product, plan, and billing cycle. List prices support evaluation. Enterprise licensing continues through consultation — most institutional buyers complete purchasing with sales rather than online checkout."
        />

        <div className="grid gap-10 lg:grid-cols-[minmax(0,1.15fr)_minmax(18rem,0.85fr)] xl:gap-14">
          <Stack gap={6} className="bg-background rounded-lg border border-border p-5 sm:p-6">
            <PlanSelector
              products={pricingProducts}
              plans={pricingPlans}
              productId={productId}
              planId={planId}
              onProductChange={(id) => {
                setProductId(id);
                const next = pricingProducts.find((item) => item.id === id);
                const first = next?.planIds[0];
                if (first) setPlanId(first);
              }}
              onPlanChange={setPlanId}
            />
            <BillingToggle
              cycles={pricingBillingCycles}
              value={cycleId}
              onChange={setCycleId}
            />
            <label className="flex max-w-xs flex-col gap-2">
              <Text as="span" size="caption" weight="medium" tone="muted">
                Quantity
              </Text>
              <input
                type="number"
                min={1}
                max={99}
                value={quantity}
                onChange={(event) => {
                  setQuantity(
                    Math.max(1, Math.min(99, Number(event.target.value) || 1)),
                  );
                }}
                className="border-border bg-background focus-visible:ring-ring rounded-md border px-3 py-2 text-small focus-visible:ring-2 focus-visible:outline-none"
              />
            </label>
            <Text as="p" size="caption" tone="muted">
              Available plans for this product: {productPlans.length}
            </Text>
          </Stack>

          <aside className="lg:sticky lg:top-24 lg:self-start">
            <OrderSummary cart={cart} />
          </aside>
        </div>

        {cart.requiresSales ? (
          <EnterpriseCta
            offering={{
              ...enterpriseOffering,
              title: 'Enterprise licensing required',
              description:
                'This selection requires enterprise licensing. Continue with a guided commercial conversation rather than self-serve checkout.',
            }}
            background="subtle"
            reveal={false}
          />
        ) : (
          <Stack direction="horizontal" gap={3} wrap>
            <button
              type="button"
              className="bg-primary text-primary-foreground focus-visible:ring-ring rounded-md px-4 py-2 text-small font-medium focus-visible:ring-2 focus-visible:outline-none"
              onClick={() => {
                router.push(`/checkout/billing?${query}`);
              }}
            >
              Continue to billing
            </button>
            <MarketingCtaLink
              label="Back to pricing"
              href="/pricing"
              variant="outline"
            />
          </Stack>
        )}
      </Stack>
    </Section>
  );
}
