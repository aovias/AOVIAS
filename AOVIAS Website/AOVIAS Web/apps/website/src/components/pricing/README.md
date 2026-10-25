# Pricing & Commerce Components

Reusable, product-agnostic pricing presentation for AOVIAS products.

Import: `import { PricingGrid, BillingToggle, … } from '@/components/pricing'`

## Pricing Domain Layer

Models in `models.ts`: `Product`, `PricingPlan`, `BillingCycle`, `Currency`, `Feature`, `FeatureCategory`, `PlanComparison`, `EnterpriseOffering`, `Addon`, `License`, `PricingFaqItem`, `PricingBannerMessage`.

UI components render these models — pricing logic stays in data / `utils.ts`.

Showcase: `/internal/design-system/pricing`

See `docs/design-system/pricing-components.md`.
