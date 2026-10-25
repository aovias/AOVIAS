import { ArrowDown, ArrowRight } from 'lucide-react';

import type { IndustryComparison } from '@/features/industries/editorial/types';
import { cn } from '@/lib/utils';

export type ComparisonBandProps = {
  comparison: IndustryComparison;
  className?: string;
};

export function ComparisonBand({ comparison, className }: ComparisonBandProps) {
  return (
    <div
      className={cn(
        'grid gap-6 md:grid-cols-[1fr_auto_1fr] md:items-stretch md:gap-4 lg:gap-8',
        className,
      )}
    >
      <ComparisonColumn
        label={comparison.traditionalLabel}
        items={comparison.traditional}
        tone="muted"
      />
      <div
        className="text-muted-foreground flex items-center justify-center"
        aria-hidden
      >
        <ArrowDown className="size-5 md:hidden" />
        <ArrowRight className="hidden size-5 md:block" />
      </div>
      <ComparisonColumn
        label={comparison.modernLabel}
        items={comparison.modern}
        tone="accent"
      />
    </div>
  );
}

function ComparisonColumn({
  label,
  items,
  tone,
}: {
  label: string;
  items: readonly string[];
  tone: 'muted' | 'accent';
}) {
  return (
    <div
      className={cn(
        'border-border rounded-xl border p-5 sm:p-6',
        tone === 'accent'
          ? 'bg-card/80 shadow-[0_0_20px_rgba(74,114,173,0.14)]'
          : 'bg-surface-muted/50',
      )}
    >
      <p
        className={cn(
          'text-eyebrow mb-4 font-semibold tracking-wide uppercase',
          tone === 'accent' ? 'text-domain-accent' : 'text-muted-foreground',
        )}
      >
        {label}
      </p>
      <ol className="space-y-3">
        {items.map((item, index) => (
          <li
            key={item}
            className="border-border/70 text-foreground flex items-start gap-3 border-b pb-3 text-small last:border-0 last:pb-0 sm:text-body"
          >
            <span
              className={cn(
                'mt-0.5 inline-flex size-6 shrink-0 items-center justify-center rounded-md text-caption font-semibold',
                tone === 'accent'
                  ? 'bg-domain-accent/15 text-domain-accent'
                  : 'bg-muted text-muted-foreground',
              )}
            >
              {index + 1}
            </span>
            <span className="leading-snug font-medium">{item}</span>
          </li>
        ))}
      </ol>
    </div>
  );
}
