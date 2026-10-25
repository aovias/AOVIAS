# Phase V6 — Enterprise Industry Experience

## Summary

Four industry hubs were rebuilt as distinct institutional narratives on one shared AOVIAS Platform — not four recolored marketing pages.

## Framework

`features/industries/editorial/`

- `IndustryEditorialTemplate` — shared editorial rhythm
- Content modules for Healthcare, Government, Manufacturing, Financial Services

**Rhythm:**  
Hero → Industry reality → Challenges → Operational workflow → Solution portfolio (Command / Studio / Platform) → Enterprise journey → Scenario narratives → CTA (Demo · Talk · Explore Products)

## Industries delivered

| Industry | Route | Atmosphere | DomainScene |
|----------|-------|------------|-------------|
| Healthcare | `/solutions/healthcare` | `aurora` (Clinical Horizon) | `healthcare` + `operations` |
| Government | `/solutions/government` | `blueprint-grid` (Civic Blueprint) | `government` + `architecture` |
| Manufacturing | `/solutions/manufacturing` | `architecture` (Industrial Precision) | `manufacturing` + `deployment` |
| Financial Services | `/solutions/financial-services` | `trust-geometry` (Institutional Trust) | `financial` + `trust` |

## Platform coherence

Each page shows Command, Studio, and Platform with **sector-specific roles** — same products, different institutional jobs. Manufacturing and Financial Services promoted to `available` in the catalog and primary Solutions mega-menu.

## Visual system

- New `DomainScene` variants: `manufacturing`, `financial`
- Industry accent tokens: `--industry-manufacturing`, `--industry-financial`
- Education / Enterprise remain on classic `IndustryTemplate`

## Out of scope

- Products / Technology redesign
- Fabricated customer logos or case studies (scenario narratives only)

## Verification

`npm run typecheck`, `npm run lint`, `npm run build` from `apps/website`.
