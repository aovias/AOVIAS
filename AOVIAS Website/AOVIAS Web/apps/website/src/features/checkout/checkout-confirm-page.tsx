'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';

import { CheckoutConfirmForm } from '@/components/forms';
import { Section } from '@/components/layout';
import { MarketingCtaLink, SectionHeader } from '@/components/marketing';
import { Stack, Text } from '@/components/primitives';
import { EmptyState, LoadingState } from '@/components/system-states';
import {
  resolveCart,
  selectionToQuery,
  type CheckoutSelection,
} from '@/features/checkout/cart-query';
import { CheckoutSteps } from '@/features/checkout/checkout-steps';
import { OrderSummary } from '@/features/checkout/order-summary';
import type {
  CheckoutConfirmFormValues,
  FormSubmitHandler,
} from '@/features/forms';
import {
  createConfirmSubmitHandler,
  loadBillingProfile,
  type BillingProfile,
} from '@/services/checkout';

export type CheckoutConfirmPageProps = {
  selection: CheckoutSelection;
};

type Gate = 'checking' | 'ready' | 'needs-billing' | 'needs-cart';

/**
 * Confirmation — purchase summary, terms, privacy acknowledgement.
 * Always paints a stable shell so empty→content flashes do not inflate CLS.
 */
export function CheckoutConfirmPage({ selection }: CheckoutConfirmPageProps) {
  const router = useRouter();
  const query = selectionToQuery(selection);
  const cart = resolveCart(selection);
  const [billing, setBilling] = useState<BillingProfile | null>(null);
  const [gate, setGate] = useState<Gate>('checking');

  useEffect(() => {
    if (!cart || cart.requiresSales) {
      setGate('needs-cart');
      return;
    }
    const profile = loadBillingProfile();
    if (!profile) {
      setGate('needs-billing');
      return;
    }
    setBilling(profile);
    setGate('ready');
  }, [cart]);

  const onSubmit: FormSubmitHandler<CheckoutConfirmFormValues> = async (
    values,
  ) => {
    if (!cart) {
      return { ok: false, message: 'Cart is unavailable.' };
    }
    const result = await createConfirmSubmitHandler(cart)(values);
    if (result.ok) {
      router.push(`/checkout/success?${query}`);
    }
    return result;
  };

  return (
    <Section
      spacing="md"
      className="bg-muted/20 -mx-4 px-4 sm:-mx-6 sm:px-6 lg:-mx-8 lg:px-8"
    >
      <Stack gap={8}>
        <CheckoutSteps current="confirm" query={query} />
        <SectionHeader
          eyebrow="Next steps"
          heading="Review commercial summary"
          description="Confirm selected plan, organization details, and acknowledgements. Payment is not processed on this public site until Commerce is live. Enterprise buyers may prefer continuing with sales."
        />

        {gate === 'checking' ? (
          <div className="min-h-[28rem]">
            <LoadingState message="Preparing confirmation…" size="md" />
          </div>
        ) : null}

        {gate === 'needs-cart' ? (
          <EmptyState
            title="Checkout selection required"
            description="Choose a self-serve product and plan before confirming an order."
            variant="default"
            reveal={false}
            primaryAction={{
              label: 'Return to checkout',
              href: `/checkout?${query}`,
            }}
            secondaryAction={{ label: 'View pricing', href: '/pricing' }}
          />
        ) : null}

        {gate === 'needs-billing' ? (
          <EmptyState
            title="Billing information required"
            description="Add organization and billing details before the final review."
            variant="default"
            reveal={false}
            primaryAction={{
              label: 'Enter billing information',
              href: `/checkout/billing?${query}`,
            }}
            secondaryAction={{
              label: 'Back to checkout',
              href: `/checkout?${query}`,
            }}
          />
        ) : null}

        {gate === 'ready' && cart && billing ? (
          <>
            <div className="grid gap-10 lg:grid-cols-[minmax(0,1.15fr)_minmax(18rem,0.85fr)] xl:gap-14">
              <Stack gap={6} className="bg-background rounded-lg border border-border p-5 sm:p-6">
                <div className="border-border bg-muted/30 rounded-lg border px-4 py-4">
                  <Text as="p" weight="medium" className="mb-3">
                    Billing profile
                  </Text>
                  <Stack gap={1}>
                    <Text as="p" size="small">
                      {billing.customer.name} · {billing.customer.email}
                    </Text>
                    <Text as="p" size="small" tone="muted">
                      {billing.organization.name}
                      {billing.organization.taxIdentifier
                        ? ` · Tax ID ${billing.organization.taxIdentifier}`
                        : ''}
                    </Text>
                    <Text as="p" size="small" tone="muted">
                      {billing.address.line1}
                      {billing.address.line2
                        ? `, ${billing.address.line2}`
                        : ''}
                    </Text>
                    <Text as="p" size="small" tone="muted">
                      {billing.address.city}
                      {billing.address.region
                        ? `, ${billing.address.region}`
                        : ''}{' '}
                      {billing.address.postalCode} · {billing.address.country}
                    </Text>
                    <Text as="p" size="caption" tone="muted">
                      Purchase type: {billing.purchaseType} · Currency:{' '}
                      {billing.currencyCode}
                    </Text>
                  </Stack>
                </div>
                <CheckoutConfirmForm onSubmit={onSubmit} />
              </Stack>
              <aside className="lg:sticky lg:top-24 lg:self-start">
                <OrderSummary cart={cart} />
              </aside>
            </div>
            <MarketingCtaLink
              label="Edit billing"
              href={`/checkout/billing?${query}`}
              variant="outline"
            />
          </>
        ) : null}

        {gate !== 'ready' && cart && !cart.requiresSales ? (
          <OrderSummary cart={cart} />
        ) : null}
      </Stack>
    </Section>
  );
}
