# 2 — Hero Implementation Report

**Phase:** V3  
**Hero ID:** `hero-home`  
**Frames:** `01`, `f1`, `n10`

## Implemented

| Requirement | Implementation |
|-------------|----------------|
| Enterprise atmosphere | `HomeHeroAtmosphere` → AVLS `enterprise` (`enterprise-abstract`) rich |
| AVLS typography | Existing Hero tokens (`h1`, eyebrow, body) |
| EBAS background | Named BrandAtmosphere, quietRight |
| Premium composition | Asymmetric copy left / visual right |
| Product ecosystem visual | `RegisteredIllustration id="ill-platform-core"` → `PlatformOrchestration` |
| CTA hierarchy | Primary demo + secondary products |
| Illustration integration | Registry pipeline |
| Hero motion | `HorizonReveal` — platinum line + content fade (≤800ms), reduced-motion safe |

## Files

- `components/marketing/home-hero-atmosphere.tsx`
- `components/marketing/horizon-reveal.tsx`
- `app/(marketing)/home-view.tsx`
- `components/brand/platform-orchestration.tsx` (industry endpoints aligned)

## Outcome

Hero meets AVLS hero budget: brand signal + headline + sentence + CTAs + one dominant visual. Horizon signature active.
