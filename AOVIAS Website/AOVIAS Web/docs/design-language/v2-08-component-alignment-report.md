# 8 — Component Alignment Report

**Phase:** V2 GEI · Release 1.1  
**Status:** Complete  
**Scope:** Align shared components to AVLS — no redesign

## Touched

| Component | Alignment |
|-----------|-----------|
| Button | AVLS easing; press scale ≤ motionScale.press |
| Card (interactive) | 4px lift max; AVLS easing; reduced-motion safe |
| BrandAtmosphere | Accepts AVLS atmosphere IDs |
| Hero / ProductHero | Resolve AVLS or live EBAS keys |
| SectionSurface | Resolve AVLS or live EBAS keys |
| Search / Header / Footer | See reports 2–4 |
| MediaSlot / Badge / Input / Breadcrumb / Quote / Trust / Statistic / CTA | Unchanged structure; inherit shell tokens |

## Explicitly not redesigned

Cards, heroes, CTAs, and page sections keep existing composition.  
Only motion/token/atmosphere resolution polish was applied where shared.

## Governance

- One accent per viewport remains a page-composition rule (V3+)
- No hardcoded artwork introduced in shared components
