'use client';

import { useRouter } from 'next/navigation';

import { BillingForm } from '@/components/forms';
import { Section } from '@/components/layout';
import { MarketingCtaLink, SectionHeader } from '@/components/marketing';
import { Stack } from '@/components/primitives';
import { EmptyState } from '@/components/system-states';
import {
  resolveCart,
  selectionToQuery,
  type CheckoutSelection,
} from '@/features/checkout/cart-query';
import { CheckoutSteps } from '@/features/checkout/checkout-steps';
import { OrderSummary } from '@/features/checkout/order-summary';
import type { BillingFormSchemaValues, FormSubmitHandler } from '@/features/forms';
import { createBillingSubmitHandler } from '@/services/checkout';

export type CheckoutBillingPageProps = {
  selection: CheckoutSelection;
};

/**
 * Billing Information — Form Engine + order summary.
 */
export function CheckoutBillingPage({ selection }: CheckoutBillingPageProps) {
  const router = useRouter();
  const query = selectionToQuery(selection);
  const cart = resolveCart(selection);

  if (!cart || cart.requiresSales) {
    return (
      <Section spacing="md">
        <Stack gap={8}>
          <CheckoutSteps current="billing" query={query} />
          <EmptyState
            title="Self-serve billing unavailable"
            description="Select a self-serve plan to continue, or contact Enterprise Sales for custom agreements."
            variant="default"
            reveal={false}
            primaryAction={{
              label: 'Return to checkout',
              href: `/checkout?${query}`,
            }}
            secondaryAction={{
              label: 'Contact enterprise',
              href: '/company/contact#form-enterprise',
            }}
          />
        </Stack>
      </Section>
    );
  }

  const onSubmit: FormSubmitHandler<BillingFormSchemaValues> = async (
    values,
  ) => {
    const result = await createBillingSubmitHandler()(values);
    if (result.ok) {
      router.push(`/checkout/confirm?${query}`);
    }
    return result;
  };

  return (
    <Section spacing="md">
      <Stack gap={8}>
        <CheckoutSteps current="billing" query={query} />
        <SectionHeader
          eyebrow="Organization details"
          heading="Contact and organization information"
          description="Provide institutional billing and contact details. Tax identifiers are optional until tax calculation is connected. This step prepares a commercial request — not a consumer purchase."
        />
        <div className="grid gap-10 lg:grid-cols-[minmax(0,1.2fr)_minmax(0,0.8fr)]">
          <BillingForm onSubmit={onSubmit} />
          <OrderSummary cart={cart} />
        </div>
        <MarketingCtaLink
          label="Back to checkout"
          href={`/checkout?${query}`}
          variant="outline"
        />
      </Stack>
    </Section>
  );
}
