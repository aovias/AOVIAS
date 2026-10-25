import { Check, Minus, MessageCircle } from 'lucide-react';

import type {
  Feature,
  FeatureAvailability,
  FeatureCategory,
  PricingPlan,
} from '@/components/pricing/models';
import { Stack, Text } from '@/components/primitives';
import { cn } from '@/lib/utils';

/**
 * FeatureComparison — accessible matrix with desktop table + mobile cards.
 */
export type FeatureComparisonProps = {
  plans: readonly PricingPlan[];
  features: readonly Feature[];
  categories?: readonly FeatureCategory[];
  /** Feature ids to include; defaults to all provided features. */
  featureIds?: readonly string[];
  className?: string;
  caption?: string;
};

function availabilityFor(
  plan: PricingPlan,
  featureId: string,
): FeatureAvailability {
  return (
    plan.features.find((item) => item.featureId === featureId)?.availability ??
    'excluded'
  );
}

function noteFor(plan: PricingPlan, featureId: string): string | undefined {
  return plan.features.find((item) => item.featureId === featureId)?.note;
}

function AvailabilityCell({
  availability,
  note,
}: {
  availability: FeatureAvailability;
  note?: string;
}) {
  if (availability === 'included') {
    return (
      <span className="inline-flex items-center gap-1">
        <Check className="text-primary size-4" aria-hidden="true" />
        <span className="absolute -m-px size-px overflow-hidden whitespace-nowrap border-0 p-0 [clip:rect(0,0,0,0)]">
          Included
        </span>
        {note ? (
          <Text as="span" size="caption" tone="muted">
            {note}
          </Text>
        ) : null}
      </span>
    );
  }
  if (availability === 'limited' || availability === 'addon') {
    return (
      <Text as="span" size="caption">
        {note ?? (availability === 'addon' ? 'Add-on' : 'Limited')}
      </Text>
    );
  }
  if (availability === 'contact') {
    return (
      <span className="inline-flex items-center gap-1">
        <MessageCircle className="size-4" aria-hidden="true" />
        <Text as="span" size="caption">
          {note ?? 'Contact'}
        </Text>
      </span>
    );
  }
  return (
    <span className="inline-flex items-center gap-1">
      <Minus className="text-muted-foreground size-4" aria-hidden="true" />
      <span className="absolute -m-px size-px overflow-hidden whitespace-nowrap border-0 p-0 [clip:rect(0,0,0,0)]">
        Not included
      </span>
    </span>
  );
}

export function FeatureComparison({
  plans,
  features,
  categories,
  featureIds,
  className,
  caption = 'Plan feature comparison',
}: FeatureComparisonProps) {
  const selectedIds = featureIds ?? features.map((feature) => feature.id);
  const featureMap = new Map(features.map((feature) => [feature.id, feature]));
  const orderedFeatures = selectedIds
    .map((id) => featureMap.get(id))
    .filter((feature): feature is Feature => Boolean(feature));

  const grouped: { category: FeatureCategory | null; items: Feature[] }[] = [];
  if (categories && categories.length > 0) {
    const sorted = [...categories].sort(
      (a, b) => (a.order ?? 0) - (b.order ?? 0),
    );
    for (const category of sorted) {
      const items = orderedFeatures.filter(
        (feature) => feature.categoryId === category.id,
      );
      if (items.length > 0) grouped.push({ category, items });
    }
    const uncategorized = orderedFeatures.filter(
      (feature) =>
        !feature.categoryId ||
        !categories.some((category) => category.id === feature.categoryId),
    );
    if (uncategorized.length > 0) {
      grouped.push({ category: null, items: uncategorized });
    }
  } else {
    grouped.push({ category: null, items: orderedFeatures });
  }

  return (
    <div className={cn('w-full', className)}>
      {/* Desktop / tablet table */}
      <div className="border-border hidden overflow-x-auto rounded-lg border md:block">
        <table className="w-full min-w-[40rem] border-collapse text-left">
          <caption className="absolute -m-px size-px overflow-hidden whitespace-nowrap border-0 p-0 [clip:rect(0,0,0,0)]">
            {caption}
          </caption>
          <thead>
            <tr className="border-border bg-muted/40 border-b">
              <th scope="col" className="text-small px-4 py-3 font-medium">
                Feature
              </th>
              {plans.map((plan) => (
                <th
                  key={plan.id}
                  scope="col"
                  className="text-small px-4 py-3 font-medium"
                >
                  {plan.name}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {grouped.map((group) => (
              <FragmentGroup
                key={group.category?.id ?? 'uncategorized'}
                group={group}
                plans={plans}
              />
            ))}
          </tbody>
        </table>
      </div>

      {/* Mobile card view */}
      <Stack gap={4} className="md:hidden">
        {plans.map((plan) => (
          <article
            key={plan.id}
            className="border-border bg-card rounded-lg border p-4"
          >
            <Text as="p" weight="semibold" className="mb-3">
              {plan.name}
            </Text>
            <dl className="flex flex-col gap-3">
              {orderedFeatures.map((feature) => {
                const availability = availabilityFor(plan, feature.id);
                const note = noteFor(plan, feature.id);
                return (
                  <div
                    key={`${plan.id}-${feature.id}`}
                    className="flex items-start justify-between gap-3"
                  >
                    <dt>
                      <Text as="span" size="small">
                        {feature.name}
                      </Text>
                    </dt>
                    <dd className="text-end">
                      <AvailabilityCell
                        availability={availability}
                        {...(note !== undefined ? { note } : {})}
                      />
                    </dd>
                  </div>
                );
              })}
            </dl>
          </article>
        ))}
      </Stack>
    </div>
  );
}

function FragmentGroup({
  group,
  plans,
}: {
  group: { category: FeatureCategory | null; items: Feature[] };
  plans: readonly PricingPlan[];
}) {
  return (
    <>
      {group.category ? (
        <tr className="border-border bg-muted/20 border-b">
          <th
            scope="colgroup"
            colSpan={plans.length + 1}
            className="text-caption text-muted-foreground px-4 py-2 font-medium tracking-wide uppercase"
          >
            {group.category.name}
          </th>
        </tr>
      ) : null}
      {group.items.map((feature) => (
        <tr key={feature.id} className="border-border border-b last:border-b-0">
          <th scope="row" className="text-small px-4 py-3 font-normal">
            <Stack gap={1}>
              <span>{feature.name}</span>
              {feature.description ? (
                <Text as="span" size="caption" tone="muted">
                  {feature.description}
                </Text>
              ) : null}
            </Stack>
          </th>
          {plans.map((plan) => {
            const availability = availabilityFor(plan, feature.id);
            const note = noteFor(plan, feature.id);
            return (
              <td key={`${plan.id}-${feature.id}`} className="px-4 py-3">
                <AvailabilityCell
                  availability={availability}
                  {...(note !== undefined ? { note } : {})}
                />
              </td>
            );
          })}
        </tr>
      ))}
    </>
  );
}
