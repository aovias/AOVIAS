# AOVIAS Motion Registry

Stable, approved animation names for the public website.

**Rule:** Future components import named registry entries (or helpers that resolve them). Inline Framer Motion animation objects in features are prohibited.

Import:

```ts
import {
  motionRegistry,
  getRegistryVariants,
  getHoverMotion,
  MotionReveal,
  MotionFade,
  MotionHover,
} from '@/lib/motion';
```

## Variant names

| Name | Description | Typical usage |
| --- | --- | --- |
| `fadeIn` | Opacity entrance | Overlays, soft mounts |
| `fadeOut` | Opacity exit | Dismissal |
| `fadeUp` | Primary entrance (opacity + y) | Sections, cards |
| `fadeDown` | Downward travel fade | Captions |
| `slideLeft` / `slideRight` | Horizontal reveal | Directional panels |
| `scaleIn` / `scaleOut` | Subtle scale (≤2%) | Dialogs |
| `staggerContainer` / `staggerItem` | Stagger orchestration | Lists |
| `reveal` | Generic progressive disclosure | Default scroll reveal |
| `sectionReveal` | Section-level reveal | Page sections |
| `pageTransition` | Route enter/exit shell | Future router wrappers |

## Hover names

| Name | Description |
| --- | --- |
| `cardHover` → `getHoverMotion('card')` | Subtle lift |
| `buttonHover` → `getHoverMotion('button')` | Micro scale |
| `linkHover` / `navItemHover` | Soft opacity |
| `interactiveHover` | Generic interactive |

## Reduced motion

`MotionProvider` sets Framer `MotionConfig reducedMotion="user"`. Helpers (`withReducedMotion`, `getFadeVariant`, reveal/fade components) collapse decorative travel to opacity-only / near-instant transitions.

## Living demo

`/dev/design-system` — Motion Foundation section.
