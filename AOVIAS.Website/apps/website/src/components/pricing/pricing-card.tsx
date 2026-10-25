import { Check } from 'lucide-react';

import { MarketingCtaLink } from '@/components/marketing';
import type {
  BillingCycle,
  Currency,
  PricingPlan,
} from '@/components/pricing/models';
import {
  formatPlanPrice,
  getPlanPrice,
} from '@/components/pricing/utils';
import {
  Badge,
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
  Stack,
  Text,
} from '@/components/primitives';
import { cn } from '@/lib/utils';

/**
 * PricingCard — single plan presentation from PricingPlan domain model.
 */
export type PricingCardProps = {
  plan: PricingPlan;
  cycle: BillingCycle;
  currencies: readonly Currency[];
  className?: string;
  /** Override highlight bullets; defaults to plan.highlights. */
  highlights?: readonly string[];
};

export function PricingCard({
  plan,
  cycle,
  currencies,
  className,
  highlights,
}: PricingCardProps) {
  const price = getPlanPrice(plan, cycle.id);
  const displayPrice = formatPlanPrice(price, currencies);
  const isEnterprise = plan.variant === 'enterprise';
  const bullets = highlights ?? plan.highlights ?? [];

  return (
    <Card
      variant={plan.featured ? 'elevated' : 'default'}
      {...(plan.featured ? { accent: 'premium' as const } : {})}
      className={cn(
        'flex h-full flex-col',
        plan.featured &&
          'border-primary/40 ring-primary/20 shadow-floating ring-2',
        isEnterprise && 'bg-surface-muted',
        className,
      )}
    >
      <CardHeader className="gap-3">
        <Stack
          direction="horizontal"
          justify="between"
          align="start"
          gap={2}
          className="w-full"
        >
          <CardTitle>{plan.name}</CardTitle>
          {plan.badge ? (
            <Badge variant={plan.featured ? 'primary' : 'secondary'}>
              {plan.badge}
            </Badge>
          ) : null}
        </Stack>
        {plan.description ? (
          <CardDescription>{plan.description}</CardDescription>
        ) : null}
        <div>
          <Text
            as="p"
            size="body-lg"
            weight="semibold"
            className={cn(
              'tracking-tight',
              plan.featured ? 'text-primary text-h3' : 'text-h4',
            )}
          >
            {displayPrice}
          </Text>
          {price?.amount !== null && price !== undefined ? (
            <Text as="p" size="caption" tone="muted">
              {cycle.id === 'annual'
                ? 'per year · billed annually'
                : `per ${cycle.label.toLowerCase()}`}
            </Text>
          ) : (
            <Text as="p" size="caption" tone="muted">
              Tailored pricing for your organization
            </Text>
          )}
        </div>
      </CardHeader>

      <CardContent className="flex-1">
        {bullets.length > 0 ? (
          <ul className="flex flex-col gap-2">
            {bullets.map((item) => (
              <li key={item} className="flex items-start gap-2">
                <Check
                  aria-hidden="true"
                  className="text-primary mt-0.5 size-4 shrink-0"
                />
                <Text as="span" size="small">
                  {item}
                </Text>
              </li>
            ))}
          </ul>
        ) : null}
      </CardContent>

      {(plan.cta || plan.secondaryCta) && (
        <CardFooter className="flex flex-col gap-2 sm:flex-row">
          {plan.cta ? (
            <MarketingCtaLink
              {...plan.cta}
              variant={isEnterprise ? 'outline' : plan.featured ? 'primary' : 'secondary'}
              className="w-full sm:w-auto"
            />
          ) : null}
          {plan.secondaryCta ? (
            <MarketingCtaLink
              {...plan.secondaryCta}
              variant="ghost"
              className="w-full sm:w-auto"
            />
          ) : null}
        </CardFooter>
      )}
    </Card>
  );
}
