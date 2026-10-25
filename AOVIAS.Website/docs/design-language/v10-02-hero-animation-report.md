# Phase V10 — Hero Animation Report

## Intent

Heroes establish presence as one composed experience — not independent element theatrics.

## Implementation

| Element | Motion |
|---|---|
| Atmosphere | `.em-atmosphere-enter` opacity fade (~700ms) |
| Eyebrow / title / description | `.em-hero-compose` fade + 8px rise |
| CTAs | `.em-hero-compose-cta` delayed sequencing |
| Illustration | `.em-hero-compose-illustration` delayed emergence |
| Homepage signature | `HorizonReveal` platinum line + content (≤800ms) |

## Constraints

- Client scroll-reveal remains off by default on heroes (LCP protection)
- CSS entrance runs without hydration delay
- `prefers-reduced-motion` disables all hero animations
