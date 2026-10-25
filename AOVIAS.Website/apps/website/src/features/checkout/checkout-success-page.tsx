'use client';

import { useEffect, useState } from 'react';

import { Section } from '@/components/layout';
import { MarketingCtaLink, SectionHeader } from '@/components/marketing';
import { EmptyState } from '@/components/system-states';
import { Stack, Text } from '@/components/primitives';
import {
  resolveCart,
  selectionToQuery,
  type CheckoutSelection,
} from '@/features/checkout/cart-query';
import { CheckoutSteps } from '@/features/checkout/checkout-steps';
import { loadBillingProfile, loadLastOrderId } from '@/services/checkout';

export type CheckoutSuccessPageProps = {
  selection: CheckoutSelection;
};

/**
 * Success — next steps, product access, support, invoice placeholder.
 */
export function CheckoutSuccessPage({ selection }: CheckoutSuccessPageProps) {
  const query = selectionToQuery(selection);
  const cart = resolveCart(selection);
  const [orderId, setOrderId] = useState<string | null>(null);
  const [email, setEmail] = useState<string | null>(null);

  useEffect(() => {
    setOrderId(loadLastOrderId());
    setEmail(loadBillingProfile()?.customer.email ?? null);
  }, []);

  const line = cart?.lines[0];

  return (
    <Section spacing="md">
      <Stack gap={8}>
        <CheckoutSteps current="success" query={query} />
        <EmptyState
          title="Commercial request recorded"
          description="Thank you. Your selection was recorded as a commercial request. Payment processing is not enabled in this environment — no charge was made. Most enterprise buyers continue with a guided sales conversation."
          variant="default"
          reveal={false}
          primaryAction={{
            label: line ? `Open ${line.product.name}` : 'View products',
            href: line ? `/products/${line.product.id}` : '/products',
          }}
          secondaryAction={{
            label: 'Contact enterprise',
            href: '/company/contact#form-enterprise',
          }}
        />

        <div className="border-border bg-muted/20 rounded-lg border px-5 py-5">
          <SectionHeader
            eyebrow="Next steps"
            heading="What happens next"
            description="When payment providers are connected, this page will confirm payment and unlock provisioning. Until then, treat this as the start of a commercial conversation."
          />
          <ul className="text-small text-muted-foreground mt-6 list-disc space-y-2 ps-5">
            <li>
              Reference:{' '}
              <Text as="span" weight="medium" tone="default">
                {orderId ?? 'Pending reference'}
              </Text>
            </li>
            {email ? <li>Confirmation contact: {email}</li> : null}
            {line ? (
              <li>
                Selected: {line.product.name} · {line.plan.name} ·{' '}
                {line.cycle.label}
              </li>
            ) : null}
            <li>Invoice download: unavailable until billing infrastructure ships.</li>
            <li>
              Prefer a purchase order or enterprise agreement? Contact Enterprise
              Sales to continue procurement.
            </li>
          </ul>
          <Stack direction="horizontal" gap={3} wrap className="mt-6">
            <MarketingCtaLink
              label="Download invoice (unavailable)"
              href="/company/contact#form-contact"
              variant="outline"
            />
            <MarketingCtaLink
              label="Enterprise sales"
              href="/company/contact#form-enterprise"
              variant="secondary"
            />
            <MarketingCtaLink
              label="Return to pricing"
              href="/pricing"
              variant="ghost"
            />
          </Stack>
        </div>
      </Stack>
    </Section>
  );
}
