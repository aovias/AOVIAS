# Sprint 1.6 Completion Report — Motion Foundation

**Date:** 2026-07-19  
**Location:** `V:\DevelopmentRepo\AOVIAS Website\AOVIAS Web`  
**Status:** Complete (reusable motion architecture only; Sprint 1.7 not started)

---

## 1. Motion Architecture Overview

Motion is centralized under `apps/website/src/lib/motion`. Every animation must originate from:

1. **Tokens** (duration, distance, opacity, scale, stagger, blur)
2. **Transitions** (instant / fast / standard / slow / …)
3. **Variants** (fade, slide, scale, stagger, reveal, page)
4. **Motion Registry** (stable public names — required for new consumers)

Framer Motion is the standard library. `MotionProvider` wraps the app with `MotionConfig reducedMotion="user"`.

**Not in scope this sprint:** homepage, heroes, product/pricing/marketing section animation, navigation content motion.

```text
AppProviders
  └── MotionProvider (reducedMotion=user)
        └── pages / components
              └── MotionReveal | MotionFade | MotionHover | getRegistryVariants(name)
```

---

## 2. Motion Folder Structure

```text
apps/website/src/lib/motion/
├── tokens.ts           # distance, opacity, scale, rotation, stagger, blur
├── durations.ts        # instant → deliberate (+ delays, ms map)
├── easing.ts           # standard / enter / exit (+ CSS strings)
├── transitions.ts      # named Transition library
├── variants.ts         # fade/slide/scale/stagger/reveal/page
├── hover.ts            # button/card/link/nav/interactive
├── fade.ts             # section/component/dialog/card/list/image…
├── reveal.tsx          # MotionReveal + useScrollReveal
├── reduced-motion.ts   # withReducedMotion, resolveVariants
├── registry.ts         # Motion Registry (stable names)
├── utilities.ts        # getNamed* helpers
├── components.tsx      # MotionFade, MotionHover, MotionStagger*
├── provider.tsx        # MotionProvider
├── config.ts           # global motionConfig
└── index.ts            # public API

docs/design-system/motion-registry.md
```

Showcase demos live under `/dev/design-system` (Motion Foundation section).

---

## 3. Motion Token Summary

| Domain | Tokens |
| --- | --- |
| Duration | instant 100ms, fast 150ms, standard 250ms, slow 350ms, deliberate 450ms |
| Delay | none, xs, sm, md, lg |
| Distance | none → xl (0–24px) |
| Opacity | hidden, muted, soft, visible |
| Scale | none, in (0.98), out/hover (≤1.02), press |
| Rotation | none, subtle (reserved; unused by default variants) |
| Stagger | tight / default / relaxed / max (50ms cap) |
| Blur | none, soft |
| Easing | standard, enter, exit (no bounce/elastic) |

Aligned with `@aovias/design-system` motion token names.

---

## 4. Transition Library Summary

| Name | Role |
| --- | --- |
| `instant`, `fast`, `standard`, `slow`, `deliberate` | Duration ladder |
| `enter` / `exit` | Asymmetric enter/exit easing |
| `fade` / `reveal` / `hover` / `page` | Semantic presets |
| `staggerContainer*` | Child orchestration |
| `reduced` | Near-instant accessibility fallback |

Access: `getMotionTransition(name)` / `motionTransitions`.

---

## 5. Variant Inventory (+ Registry)

**Core variants:** `fadeIn`, `fadeOut`, `fadeUp`, `fadeDown`, `slideLeft`, `slideRight`, `scaleIn`, `scaleOut`, `staggerContainer`, `staggerItem`, `reveal`, `pageTransition`, `reducedFade`

**Fade system presets:** section, component, dialog, card, list, image, listItem, dismiss, overlay, caption

**Hover system:** button, card, link, navItem, interactive

**Registry names (stable):** see [`docs/design-system/motion-registry.md`](../design-system/motion-registry.md) — includes `sectionReveal`, `cardHover`, `buttonHover`, etc.

---

## 6. Accessibility Summary

- `MotionProvider` → Framer `reducedMotion="user"`
- Decorative travel collapses via `withReducedMotion` / `reducedFade`
- Hover micro-motion skipped when reduced motion is preferred
- Focus rings remain CSS-based (never animated away)
- Scroll reveal does not trap focus or block interaction
- Essential feedback preserved (near-instant opacity)

---

## 7. Performance Considerations

- Prefer **opacity + transform** only (encoded in `motionConfig.preferredProperties`)
- Avoid animating layout properties (width/height/top/left)
- Scroll reveal uses Framer `whileInView` (Intersection Observer)
- Default `once: true` to avoid replaying decorative motion
- Client components limited to motion utilities/components; tokens/variants are pure modules
- No marketing pages animated — zero unnecessary client trees on content routes from this sprint

---

## 8. Validation Results

| Check | Result |
| --- | --- |
| Motion tokens | Pass |
| Transition library | Pass |
| Variants + fade + hover | Pass |
| Scroll reveal utilities | Pass |
| Motion Registry | Pass |
| Reduced motion wiring | Pass (`MotionProvider` + helpers) |
| No inline feature animations | Pass (architecture only; showcase consumes registry) |
| TypeScript | Pass |
| ESLint | Pass |
| Production build | Pass |
| Showcase | `/dev/design-system` Motion Foundation section |

---

## Recommended next steps (Sprint 1.7+)

1. Apply registry motions to composite sections when those sprints begin — still via named registry imports
2. Optional App Router `pageTransition` wrapper (only when product needs it)
3. Unit tests for `withReducedMotion` / registry resolution
4. Lint rule or PR checklist: ban inline `transition:` / `variants:` objects outside `lib/motion`

Do **not** begin Sprint 1.7 automatically.
