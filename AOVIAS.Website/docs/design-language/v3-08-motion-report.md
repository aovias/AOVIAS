# 8 — Motion Report

**Phase:** V3  
**AVLS:** `12-motion-language.md`

## Signature

| Moment | Implementation |
|--------|----------------|
| Horizon Reveal | `HorizonReveal` — platinum line draw + content fade, `signature` 800ms |
| Section reveal | `MarketingReveal` / `MotionReveal` `sectionReveal` |
| Card / panel hover | Existing interactive card motion (≤4px) |
| CTA | Button AVLS press/hover from V2 |

## Reduced motion

`useReducedMotion` short-circuits Horizon Reveal; MotionProvider `reducedMotion="user"` remains site-wide.

## Restraint

No bounce, parallax storms, or second motion library.
