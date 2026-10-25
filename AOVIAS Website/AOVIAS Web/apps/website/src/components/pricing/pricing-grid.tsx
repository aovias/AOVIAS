import { MarketingReveal } from '@/components/marketing/reveal';
import type {
  BillingCycle,
  Currency,
  PricingPlan,
} from '@/components/pricing/models';
import { PricingCard } from '@/components/pricing/pricing-card';
import { Container, Grid, type GridCols } from '@/components/primitives';
import { cn } from '@/lib/utils';

/**
 * PricingGrid — responsive multi-plan layout with featured emphasis.
 */
export type PricingGridProps = {
  plans: readonly PricingPlan[];
  cycle: BillingCycle;
  currencies: readonly Currency[];
  cols?: Extract<GridCols, 2 | 3 | 4>;
  className?: string;
  reveal?: boolean;
  /** Optional section wrapper width. */
  contained?: boolean;
};

function resolveCols(count: number, preferred?: Extract<GridCols, 2 | 3 | 4>): Extract<GridCols, 2 | 3 | 4> {
  if (preferred) return preferred;
  if (count <= 2) return 2;
  if (count === 3) return 3;
  return 4;
}

export function PricingGrid({
  plans,
  cycle,
  currencies,
  cols,
  className,
  reveal = true,
  contained = true,
}: PricingGridProps) {
  const gridCols = resolveCols(plans.length, cols);

  const body = (
    <MarketingReveal enabled={reveal}>
      <Grid cols={gridCols} gap={6} className={cn('items-stretch', className)}>
        {plans.map((plan) => (
          <PricingCard
            key={plan.id}
            plan={plan}
            cycle={cycle}
            currencies={currencies}
          />
        ))}
      </Grid>
    </MarketingReveal>
  );

  if (!contained) return body;

  return (
    <section className="w-full py-8">
      <Container width="wide">{body}</Container>
    </section>
  );
}
