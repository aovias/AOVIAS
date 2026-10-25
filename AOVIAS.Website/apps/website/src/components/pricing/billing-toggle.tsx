'use client';

import { MotionReveal } from '@/lib/motion';
import type { BillingCycle } from '@/components/pricing/models';
import { Text } from '@/components/primitives';
import { cn } from '@/lib/utils';

/**
 * BillingToggle — accessible cycle selector (monthly / annual / future).
 * Controlled: parent updates prices when `value` changes.
 */
export type BillingToggleProps = {
  cycles: readonly BillingCycle[];
  value: string;
  onChange: (cycleId: string) => void;
  className?: string;
  label?: string;
};

export function BillingToggle({
  cycles,
  value,
  onChange,
  className,
  label = 'Billing period',
}: BillingToggleProps) {
  const active = cycles.find((cycle) => cycle.id === value);
  const discount = active?.discountLabel;

  return (
    <div className={cn('flex flex-col items-center gap-2', className)}>
      <div
        role="radiogroup"
        aria-label={label}
        className="border-border bg-muted/50 inline-flex rounded-lg border p-1"
      >
        {cycles.map((cycle, index) => {
          const selected = cycle.id === value;
          return (
            <button
              key={cycle.id}
              type="button"
              role="radio"
              aria-checked={selected}
              tabIndex={selected ? 0 : -1}
              onClick={() => onChange(cycle.id)}
              onKeyDown={(event) => {
                if (event.key !== 'ArrowRight' && event.key !== 'ArrowLeft') {
                  return;
                }
                event.preventDefault();
                const delta = event.key === 'ArrowRight' ? 1 : -1;
                const next = cycles[(index + delta + cycles.length) % cycles.length];
                if (next) onChange(next.id);
              }}
              className={cn(
                'focus-visible:ring-ring min-h-9 rounded-md px-3 py-1.5 text-small font-medium transition-colors focus-visible:ring-2 focus-visible:outline-none',
                selected
                  ? 'bg-background text-foreground shadow-sm'
                  : 'text-muted-foreground hover:text-foreground',
              )}
            >
              {cycle.label}
            </button>
          );
        })}
      </div>
      {discount ? (
        <MotionReveal name="fadeIn" once>
          <Text as="p" size="caption" tone="muted">
            {discount}
          </Text>
        </MotionReveal>
      ) : null}
    </div>
  );
}
