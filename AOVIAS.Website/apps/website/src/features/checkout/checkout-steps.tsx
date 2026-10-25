import Link from 'next/link';

import { Text } from '@/components/primitives';
import { cn } from '@/lib/utils';

export type CheckoutStepId = 'cart' | 'billing' | 'confirm' | 'success';

const steps: readonly { id: CheckoutStepId; label: string; href?: string }[] = [
  { id: 'cart', label: 'Checkout', href: '/checkout' },
  { id: 'billing', label: 'Billing', href: '/checkout/billing' },
  { id: 'confirm', label: 'Confirm', href: '/checkout/confirm' },
  { id: 'success', label: 'Success' },
];

export type CheckoutStepsProps = {
  current: CheckoutStepId;
  query: string;
};

/**
 * Lightweight step indicator for the checkout journey.
 */
export function CheckoutSteps({ current, query }: CheckoutStepsProps) {
  const currentIndex = steps.findIndex((step) => step.id === current);

  return (
    <nav aria-label="Checkout steps">
      <ol className="flex flex-wrap gap-3">
        {steps.map((step, index) => {
          const active = step.id === current;
          const complete = index < currentIndex;
          const qs = query ? `?${query}` : '';
          const content = (
            <Text
              as="span"
              size="caption"
              weight={active ? 'semibold' : 'medium'}
              tone={active ? 'default' : 'muted'}
            >
              {index + 1}. {step.label}
            </Text>
          );

          return (
            <li
              key={step.id}
              className={cn(
                'border-border rounded-md border px-3 py-1.5',
                active && 'border-primary bg-primary/5',
                complete && 'border-border',
              )}
              aria-current={active ? 'step' : undefined}
            >
              {step.href && (complete || active) ? (
                <Link
                  href={`${step.href}${qs}`}
                  className="focus-visible:ring-ring rounded-sm focus-visible:ring-2 focus-visible:outline-none"
                >
                  {content}
                </Link>
              ) : (
                content
              )}
            </li>
          );
        })}
      </ol>
    </nav>
  );
}
