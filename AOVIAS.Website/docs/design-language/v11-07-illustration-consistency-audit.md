# Phase V11 — Illustration Consistency Audit

## Registry

- Catalog IDs remain the source of truth (`registries/avlf/illustrations.ts`).
- `RegisteredIllustration` supports `componentKey`, `illustrationProps`, and `priority` (SSR only when true).
- Home / products / contact heroes and bands migrate toward registry loaders.

## Consistency checks

| Check | Result |
| --- | --- |
| Stroke / brandInk language | Pass — BrandShell family |
| Theme compatibility | Pass — light/dark tokens |
| Atmosphere compatibility | Pass — EBAS + illustrations |
| Duplicate concepts | Acceptable residual — BrandShell direct imports still certified for R1 where noted |
| Teaching value | Pass — diagrams map to platform / trust / industry meaning |

## Residual

Post-R1: finish migrating remaining direct Brand imports on technology deep pages to registry IDs for one pipeline.
