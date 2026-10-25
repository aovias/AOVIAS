# Pricing & Commerce Components API

Import: `import { PricingGrid, BillingToggle, … } from '@/components/pricing'`

Showcase: `/internal/design-system/pricing`

All commercial content is **props-only** via the Pricing Domain Layer. No product prices are hardcoded in components. Sample showcase figures are QA-only.

## Pricing Domain Layer

Defined in `apps/website/src/components/pricing/models.ts`.

| Model | Purpose |
| --- | --- |
| `Product` | Commercial product with `planIds` |
| `PricingPlan` | Plan name, prices, features, CTAs, featured/enterprise |
| `BillingCycle` | Monthly / annual / future cycles |
| `Currency` | Code, symbol, locale |
| `Feature` / `FeatureCategory` | Matrix rows + groups |
| `PlanFeature` | Availability per plan (`included` \| `excluded` \| `limited` \| `addon` \| `contact`) |
| `PlanComparison` | Declares which plans/features to compare |
| `EnterpriseOffering` | Enterprise messaging + up to 3 CTAs |
| `Addon` / `License` | Future licensing extensions |
| `PricingFaqItem` | FAQ (compatible with Trust `FaqItem`) |
| `PricingBannerMessage` | Informational notices |

Helpers in `utils.ts`: `getPlanPrice`, `formatPlanPrice`, `plansForProduct`.

## Component API

| Component | Key props | Notes |
| --- | --- | --- |
| `PricingCard` | `plan`, `cycle`, `currencies` | Featured + enterprise variants |
| `PricingGrid` | `plans[]`, `cycle`, `currencies`, `cols?` | Dynamic plan count |
| `FeatureComparison` | `plans`, `features`, `categories?` | Table + mobile cards |
| `PlanSelector` | `products`, `plans`, `productId`, `planId?` | Client; active states |
| `BillingToggle` | `cycles`, `value`, `onChange` | Client; controlled prices |
| `EnterpriseCta` | `offering` | Contact / demo / consultation |
| `PricingFaq` | `items[]` | Composes Trust `Faq` |
| `PricingBanner` | `message` | Info / success / warning / neutral |

### Usage

```tsx
'use client';

import { useState } from 'react';
import {
  BillingToggle,
  PricingGrid,
  type BillingCycle,
  type PricingPlan,
} from '@/components/pricing';

const cycles: BillingCycle[] = [
  { id: 'monthly', label: 'Monthly', months: 1 },
  { id: 'annual', label: 'Annual', months: 12, discountLabel: 'Save with annual' },
];

export function Plans({ plans }: { plans: PricingPlan[] }) {
  const [cycleId, setCycleId] = useState('annual');
  const cycle = cycles.find((c) => c.id === cycleId)!;

  return (
    <>
      <BillingToggle cycles={cycles} value={cycleId} onChange={setCycleId} />
      <PricingGrid
        plans={plans}
        cycle={cycle}
        currencies={[{ code: 'USD', symbol: '$', locale: 'en-US' }]}
      />
    </>
  );
}
```

## Motion

- Grid/Enterprise: `MarketingReveal` → `sectionReveal`
- BillingToggle discount + PlanSelector plans: Motion Registry `fadeIn`
- FAQ expansion: Trust FAQ interaction (no decorative motion)

## Accessibility

- Cards: semantic headings/lists; CTA links
- Comparison: `<table>` + caption + `scope`; mobile `<dl>` cards
- Toggle / selector: `radiogroup` / `tablist` with keyboard-focusable buttons
- Banner: Alert live region
- FAQ: Trust FAQ accordion a11y

## Responsive

- Grid: 2–4 columns via primitive `Grid`
- Comparison: table from `md`, stacked cards below
- Cards: full-width CTAs on small screens

## Theme

- Semantic tokens only (`bg-card`, `border-primary/40`, alert tones)
- Light/dark via Theme System
