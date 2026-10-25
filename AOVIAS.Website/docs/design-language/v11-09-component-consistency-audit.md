# Phase V11 — Component Consistency Audit

| Component | Spacing | Type | Radius | Motion | A11y |
| --- | --- | --- | --- | --- | --- |
| Header / HeaderChrome | Pass | Pass | Pass | CSS elevation | Pass |
| Footer | Pass | Pass | Pass | — | Pass |
| Hero | Pass | Pass | Pass | CSS compose | Pass |
| Cards (Feature / Premium / Commercial) | Pass | Pass | Pass | CSS `em-lift` | Pass |
| Buttons / MarketingCtaLink | Pass | Pass | Pass | Token durations | Pass |
| Forms | Pass | Pass | Pass | — | Pass |
| Tables | Pass | Pass | Pass | — | Pass |
| Breadcrumbs | Pass | Pass | Pass | — | Pass |
| Search | Pass | Pass | Pass | Overlay deferred | Pass |
| CTA section | Pass | Pass | Pass | No Framer | Pass |
| SectionSurface | Pass | Pass | Pass | — | Pass |
| Illustration / MediaSlot | Pass | — | Pass | Lazy default | Pass |
| Mobile drawer | Pass | Pass | Pass | CSS enter | Pass |

## Drift

Framer removed from shared card/grid/hero/CTA paths — visual hover language preserved via CSS. Mega-menu retains Framer after idle load on desktop.
