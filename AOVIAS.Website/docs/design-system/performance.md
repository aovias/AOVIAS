# Performance Foundation

Centralized performance architecture for the AOVIAS Public Website.

## Principles

- Server Components by default; minimize client JS
- Prefer `opacity` / `transform` motion (Sprint 1.6)
- Use `OptimizedImage` (`next/image`) — no raw `<img>` without justification
- Fonts via `next/font` (`lib/performance/fonts.ts`)
- Budgets and Lighthouse targets live in `config/performance.ts`

## Import

```ts
import {
  fontSans,
  lazyComponent,
  preloadImage,
  checkBudget,
} from '@/lib/performance';
import { OptimizedImage } from '@/components/media';
```

## Tooling

| Command | Purpose |
| --- | --- |
| `npm run analyze` | Bundle analyzer (opens reports after build) |
| `npm run budget:check` | Heuristic JS budget check after build |
| `npm run build && npm run start` then `npm run lighthouse` | LHCI gates |

## Budgets (gzip KB)

| Resource | Budget |
| --- | --- |
| Initial JS | ≤ 150 |
| Per-route JS | ≤ 75 |
| CSS | ≤ 50 |
| Fonts | ≤ 120 |
| Hero image | ≤ 200 |
| Third-party | ≤ 30 |

## Core Web Vitals

| Metric | Target |
| --- | --- |
| LCP | ≤ 2.5s |
| INP | ≤ 200ms |
| CLS | < 0.1 |

## Caching

Policies in `cachePolicies` — applied to `/_next/static`, images, brand/og assets, sitemap/robots via `next.config.ts` headers. Ready for CDN/edge mirroring.
