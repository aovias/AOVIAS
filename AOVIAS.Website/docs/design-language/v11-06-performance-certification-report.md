# Phase V11 — Performance Certification Report

## Targets

| Metric | Target | Desktop | Mobile (lab) |
| --- | --- | --- | --- |
| Performance | ≥95 | **100** all critical routes | **91–92** (FAIL gate) |
| Accessibility | 100 | **100** | **100** |
| Best Practices | 100 | **100** | **100** |
| SEO | 100 | **100** (auth exempt) | **100** |

Source: `apps/website/.lighthouseci-r1-4/summary.json` · `npm run lighthouse:r1.4`

## Mobile lab diagnosis

| Signal | Value |
| --- | --- |
| Simulated FCP | ~1.36 s |
| Simulated LCP | ~3.31 s |
| Observed LCP | ~0.15 s |
| TBT | ≤30 ms |
| Root cause class | Lantern LCP inflation + remaining shared JS (~framer chunk still in initial HTML graph) |

## Bundle / architecture improvements shipped

- Homepage First Load JS **172 → 128 kB**
- Below-fold illustration SSR disabled by default
- Framer isolated from Hero, CTA, cards, FeatureGrid, mobile drawer, system-states
- Search overlay + mega-menu deferred
- Font `display: optional`, sans preload off
- Shell transition is a static wrapper (no Framer)

## Certification

**Desktop Performance: CERTIFIED.**  
**Mobile Performance: NOT CERTIFIED** against ≥95 until Framer/shared initial chunks are fully purged from marketing HTML and LCP lab ≤ ~2.6 s.
