# Sprint 2.5 Completion Report — Pricing & Commerce Components

**Date:** 2026-07-19  
**Location:** `V:\DevelopmentRepo\AOVIAS Website\AOVIAS Web`  
**Status:** Complete (reusable pricing presentation only; Sprint 2.6 not started)

---

## 1. Pricing Component Inventory

| # | Component | Status |
| --- | --- | --- |
| — | **Pricing Domain Layer** (`models.ts` + `utils.ts`) | ✅ |
| 1 | Pricing Card | ✅ |
| 2 | Pricing Grid | ✅ |
| 3 | Feature Comparison | ✅ |
| 4 | Plan Selector | ✅ |
| 5 | Billing Toggle | ✅ |
| 6 | Enterprise CTA | ✅ |
| 7 | Pricing FAQ | ✅ |
| 8 | Pricing Banner | ✅ |

---

## 2. Commerce Architecture Overview

```text
apps/website/src/components/pricing/
├── models.ts                 # Pricing Domain Layer
├── utils.ts                  # getPlanPrice, formatPlanPrice, plansForProduct
├── pricing-card.tsx
├── pricing-grid.tsx
├── feature-comparison.tsx
├── plan-selector.tsx         # client
├── billing-toggle.tsx        # client
├── enterprise-cta.tsx
├── pricing-faq.tsx           # client → Trust Faq
├── pricing-banner.tsx
├── index.ts
└── README.md

Showcase: /internal/design-system/pricing
Docs: docs/design-system/pricing-components.md
```

Pricing logic lives in domain data + utils — not inside presentational components. No payments, checkout, or billing APIs.

---

## 3. Pricing Data Model Summary

| Model | Role |
| --- | --- |
| `Product` | Multi-product catalog entry |
| `PricingPlan` | Plan presentation + prices + features |
| `BillingCycle` | Extensible billing periods |
| `Currency` | Formatting metadata |
| `Feature` / `FeatureCategory` | Comparison matrix |
| `PlanFeature` | Per-plan availability |
| `PlanComparison` | Declares comparison scope |
| `EnterpriseOffering` | Enterprise CTA band content |
| `Addon` / `License` | Future licensing extensions |
| `PricingFaqItem` | Pricing FAQ (Trust-compatible) |
| `PricingBannerMessage` | Notices / announcements |

---

## 4. Component API Summary

Primary entry: `@/components/pricing`

- Cards/grids resolve display prices via `getPlanPrice` + `formatPlanPrice` for the active cycle
- `PlanSelector` / `BillingToggle` are controlled client islands
- `PricingFaq` reuses Trust FAQ (search + categories)
- Full prop tables: [`docs/design-system/pricing-components.md`](../design-system/pricing-components.md)

---

## 5. Accessibility Summary

- Semantic cards, lists, and CTA links
- Comparison: captioned table with `scope`; mobile definition lists
- Toggle/selector: `radiogroup` / `tablist` + focus rings
- Banner: polite Alert live region
- FAQ: Trust accordion keyboard/`aria-*` behaviour
- Availability icons include accessible text alternatives

---

## 6. Responsive Verification

- Pricing grid adapts 2–4 columns by plan count
- Feature comparison: table from `md`, card stack on mobile
- Card CTAs stack full-width on small screens
- Showcase builds as static route for breakpoint QA

---

## 7. Theme Compatibility Summary

- Semantic tokens only (`bg-card`, `border-primary/40`, alert tones)
- Light/dark via Theme System; showcase `ThemeToggle`
- No hardcoded brand colors or payment-brand styling

---

## 8. Validation Results

| Check | Result |
| --- | --- |
| All 8 components + domain layer | Pass |
| Primitives / tokens / motion consumed | Pass |
| Product-agnostic (no page assumptions) | Pass |
| No payments / checkout / billing APIs | Pass |
| Showcase `/internal/design-system/pricing` | Pass |
| TypeScript | Pass |
| ESLint | Pass |
| Production build | Pass |

---

## 9. Readiness Assessment for Sprint 2.6

**Ready for Sprint 2.6** once planned (likely page templates / site assembly).

Recommended next steps:

1. Assemble product pricing pages from pricing + marketing + trust + forms blocks
2. Supply real `Product` / `PricingPlan` payloads (CMS or config) — never invent commercial offers
3. Keep billing toggle state at the page/template level
4. Wire Form Engine for enterprise contact / demo CTAs

Do **not** begin Sprint 2.6 automatically.
