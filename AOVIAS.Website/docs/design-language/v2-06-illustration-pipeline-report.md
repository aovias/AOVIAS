# 6 — Illustration Pipeline Report

**Phase:** V2 GEI · Release 1.1  
**Status:** Complete (infrastructure only)  
**AVLS refs:** `05-illustration-language.md`, `registries/illustrations.md`

## Pipeline

| Capability | Implementation |
|------------|----------------|
| Registry lookup | `illustrationCatalog` / `getIllustrationEntry` |
| Component mapping | AVLS ID → brand component key |
| Theme awareness | Brand illustrations use semantic tokens (light/dark) |
| Responsive behaviour | Wrapper `w-full` + SVG fluid sizing |
| Lazy loading | `next/dynamic` + `Suspense` |
| Fallback | `IllustrationPlaceholder` |

## Public API

```tsx
import { RegisteredIllustration } from '@/components/brand';

<RegisteredIllustration id="ill-platform-core" />
```

## Non-goals (honored)

- Page illustrations were **not** replaced
- No new one-off artwork

## Files

- `registries/avlf/illustrations.ts`
- `components/brand/registered-illustration.tsx`
