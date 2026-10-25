# Phase V10 — Accessibility & Reduced Motion Report

## Layers

| Layer | Behavior |
|---|---|
| CSS tokens | All duration tokens → `0ms` under `prefers-reduced-motion` |
| Framer | `MotionConfig reducedMotion="user"` + local `useReducedMotion` gates |
| Utilities | `.em-*` animations forced off; lifts disabled |
| Focus | Focus rings retained on interactive controls |
| Keyboard | Mega-menu, drawer, search focus traps unchanged |

## Guarantee

No critical information depends solely on animation. Content remains fully readable when motion is disabled.
