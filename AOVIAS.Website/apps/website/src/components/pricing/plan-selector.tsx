'use client';

import { MotionReveal } from '@/lib/motion';
import type { PricingPlan, Product } from '@/components/pricing/models';
import { plansForProduct } from '@/components/pricing/utils';
import { Stack, Text } from '@/components/primitives';
import { cn } from '@/lib/utils';

/**
 * PlanSelector — product + plan selection with active states.
 * Supports future product expansion via Product[] models.
 */
export type PlanSelectorProps = {
  products: readonly Product[];
  plans: readonly PricingPlan[];
  productId: string;
  planId?: string;
  onProductChange: (productId: string) => void;
  onPlanChange?: (planId: string) => void;
  className?: string;
  productLabel?: string;
  planLabel?: string;
};

export function PlanSelector({
  products,
  plans,
  productId,
  planId,
  onProductChange,
  onPlanChange,
  className,
  productLabel = 'Product',
  planLabel = 'Plan',
}: PlanSelectorProps) {
  const product = products.find((item) => item.id === productId) ?? products[0];
  const available = product
    ? plansForProduct(plans, product.planIds)
    : [];

  return (
    <Stack gap={4} className={cn(className)}>
      <div>
        <Text as="p" size="caption" tone="muted" weight="medium" className="mb-2">
          {productLabel}
        </Text>
        <div
          role="radiogroup"
          aria-label={productLabel}
          className="flex flex-wrap gap-2"
        >
          {products.map((item) => {
            const selected = item.id === productId;
            return (
              <button
                key={item.id}
                type="button"
                role="radio"
                aria-checked={selected}
                tabIndex={selected ? 0 : -1}
                onClick={() => onProductChange(item.id)}
                onKeyDown={(event) => {
                  if (event.key !== 'ArrowRight' && event.key !== 'ArrowLeft') {
                    return;
                  }
                  event.preventDefault();
                  const index = products.findIndex((p) => p.id === productId);
                  const delta = event.key === 'ArrowRight' ? 1 : -1;
                  const next =
                    products[(index + delta + products.length) % products.length];
                  if (next) onProductChange(next.id);
                }}
                className={cn(
                  'focus-visible:ring-ring min-h-11 rounded-md border px-3 py-2 text-small font-medium transition-colors focus-visible:ring-2 focus-visible:outline-none',
                  selected
                    ? 'border-primary bg-primary/5 text-foreground'
                    : 'border-border text-muted-foreground hover:text-foreground hover:bg-muted/50',
                )}
              >
                {item.name}
              </button>
            );
          })}
        </div>
        {product?.description ? (
          <Text as="p" size="caption" tone="muted" className="mt-2">
            {product.description}
          </Text>
        ) : null}
      </div>

      {available.length > 0 && onPlanChange ? (
        <MotionReveal name="fadeIn" once>
          <div>
            <Text
              as="p"
              size="caption"
              tone="muted"
              weight="medium"
              className="mb-2"
            >
              {planLabel}
            </Text>
            <div
              role="radiogroup"
              aria-label={planLabel}
              className="flex flex-wrap gap-2"
            >
              {available.map((plan) => {
                const selected = plan.id === planId;
                return (
                  <button
                    key={plan.id}
                    type="button"
                    role="radio"
                    aria-checked={selected}
                    onClick={() => onPlanChange(plan.id)}
                    className={cn(
                      'focus-visible:ring-ring rounded-md border px-3 py-1.5 text-small font-medium transition-colors focus-visible:ring-2 focus-visible:outline-none',
                      selected
                        ? 'border-primary bg-background text-foreground shadow-sm'
                        : 'border-border text-muted-foreground hover:text-foreground',
                    )}
                  >
                    {plan.name}
                  </button>
                );
              })}
            </div>
          </div>
        </MotionReveal>
      ) : null}
    </Stack>
  );
}
