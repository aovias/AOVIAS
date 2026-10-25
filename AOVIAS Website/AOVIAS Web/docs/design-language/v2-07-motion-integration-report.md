# 7 — Motion Integration Report

**Phase:** V2 GEI · Release 1.1  
**Status:** Complete  
**AVLS refs:** `12-motion-language.md`

## Tokens

| Token | Duration |
|-------|----------|
| Instant | 100ms |
| Fast | 150ms |
| Standard | 250ms |
| Slow | 350ms |
| Deliberate | 450ms |
| **Signature** (new) | **800ms** (Horizon Reveal ceiling) |

## Applied surfaces

| Surface | Behaviour |
|---------|-----------|
| Shell route enter | `ShellTransition` — restrained fade/translate; first paint instant |
| Search overlay | Fast enter/exit with reduced-motion short-circuit |
| Header / mega menu | CSS ease aligned to AVLS standard curve |
| Button / interactive card | Press/hover scale capped; `motion-reduce` honored |

## Transitions registry

Added `signature` and `overlay` entries in `lib/motion/transitions.ts`.

## Principles honored

- Calm, professional, precise
- No bounce / elastic / second motion library
- `prefers-reduced-motion` respected via Framer `useReducedMotion` + CSS `motion-reduce`
