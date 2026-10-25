'use client';

import { useMemo, useState } from 'react';

import { Section } from '@/components/layout';
import { SectionHeader } from '@/components/marketing';
import {
  BillingToggle,
  FeatureComparison,
  PlanSelector,
  PricingGrid,
  plansForProduct,
} from '@/components/pricing';
import { Stack, Text } from '@/components/primitives';
import {
  pricingBillingCycles,
  pricingCurrencies,
  pricingFeatureCategories,
  pricingFeatures,
  pricingPlans,
  pricingProducts,
} from '@/features/pricing/domain';

/**
 * Interactive commerce band — PlanSelector, BillingToggle, grids, comparison.
 * Pricing logic stays in the catalog + commerce utils.
 */
export function PricingCommerceBand() {
  const [productId, setProductId] = useState(pricingProducts[0]!.id);
  const [planId, setPlanId] = useState(
    pricingProducts[0]!.planIds[0] ?? pricingPlans[0]!.id,
  );
  const [cycleId, setCycleId] = useState(pricingBillingCycles[1]!.id);

  const cycle =
    pricingBillingCycles.find((item) => item.id === cycleId) ??
    pricingBillingCycles[0]!;

  const activeProduct =
    pricingProducts.find((item) => item.id === productId) ?? pricingProducts[0]!;

  const visiblePlans = useMemo(
    () => plansForProduct(pricingPlans, activeProduct.planIds),
    [activeProduct.planIds],
  );

  const comparisonFeatures = useMemo(
    () =>
      pricingFeatures.filter(
        (feature) =>
          feature.categoryId === productId ||
          feature.categoryId === 'security' ||
          feature.categoryId === 'success',
      ),
    [productId],
  );

  const comparisonCategories = useMemo(
    () =>
      pricingFeatureCategories.filter(
        (category) =>
          category.id === productId ||
          category.id === 'security' ||
          category.id === 'success',
      ),
    [productId],
  );

  const studioPlans = useMemo(
    () =>
      plansForProduct(
        pricingPlans,
        pricingProducts.find((item) => item.id === 'studio')?.planIds ?? [],
      ),
    [],
  );

  const commandPlans = useMemo(
    () =>
      plansForProduct(
        pricingPlans,
        pricingProducts.find((item) => item.id === 'command')?.planIds ?? [],
      ),
    [],
  );

  return (
    <>
      <Section spacing="md" id="plans" className="bg-background">
        <Stack gap={8}>
          <SectionHeader
            eyebrow="Published packaging"
            heading="Review list pricing by product"
            description="Select AOVIAS Studio or AOVIAS Command, then review Evaluate, Professional, and Enterprise engagement. List prices support evaluation — enterprise licensing continues through consultation."
          />
          <PlanSelector
            products={pricingProducts}
            plans={pricingPlans}
            productId={productId}
            planId={planId}
            onProductChange={(id) => {
              setProductId(id);
              const next = pricingProducts.find((item) => item.id === id);
              if (next?.planIds[0]) setPlanId(next.planIds[0]);
            }}
            onPlanChange={setPlanId}
          />
          <BillingToggle
            cycles={pricingBillingCycles}
            value={cycleId}
            onChange={setCycleId}
          />
          <Text as="p" size="caption" tone="muted">
            Viewing {activeProduct.name}
            {cycle.discountLabel ? ` · ${cycle.discountLabel}` : null}
          </Text>
        </Stack>
      </Section>

      <Section spacing="md" id="studio-pricing" className="bg-subtle/40">
        <Stack gap={8}>
          <SectionHeader
            eyebrow="AOVIAS Studio"
            heading="Studio packaging"
            description="Evaluate, Professional, and Enterprise models for the delivery workspace. Differences reflect organizational needs."
          />
          <PricingGrid
            plans={studioPlans}
            cycle={cycle}
            currencies={pricingCurrencies}
            contained={false}
          />
        </Stack>
      </Section>

      <Section spacing="md" id="command-pricing" className="bg-background">
        <Stack gap={8}>
          <SectionHeader
            eyebrow="AOVIAS Command"
            heading="Command packaging"
            description="Professional list pricing and Enterprise licensing for institutional purchasing. Multi-site and procurement constraints continue through consultation."
          />
          <PricingGrid
            plans={commandPlans}
            cycle={cycle}
            currencies={pricingCurrencies}
            contained={false}
          />
        </Stack>
      </Section>

      <Section spacing="md" id="feature-comparison" className="bg-muted/30">
        <Stack gap={8}>
          <SectionHeader
            eyebrow="Compare"
            heading={`Capability matrix — ${activeProduct.name}`}
            description="Procurement-oriented feature summary for the selected product. Switch products above to compare Studio or Command packaging."
          />
          <FeatureComparison
            plans={visiblePlans}
            features={comparisonFeatures}
            categories={comparisonCategories}
          />
        </Stack>
      </Section>
    </>
  );
}
