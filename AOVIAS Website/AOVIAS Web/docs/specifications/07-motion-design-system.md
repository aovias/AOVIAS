---
title: "AOVIAS Motion Design System"
source: appflowy
workspace: AOVIAS Website
space: General
view_id: "b5269d6d-95d6-438b-8d33-88a2c4f5b750"
exported_at: "2026-07-19"
---

# AOVIAS Motion Design System
### Purpose

This document establishes the global motion language for the AOVIAS Public Website.

Motion is a functional design element used to:

- Guide attention

- Improve usability

- Reinforce hierarchy

- Communicate state changes

- Create continuity

- Enhance perceived quality

Motion shall never exist purely for decoration.

Every animation should have a clear purpose.

---

## 1. Motion Philosophy

Motion should feel:

- Calm

- Professional

- Precise

- Deliberate

- Confident

- Natural

Motion should never feel:

- Playful

- Bouncy

- Chaotic

- Distracting

- Overly futuristic

- Game-like

Visitors should remember the experience—not the animation.

---

## 2. Motion Principles

Every animation must satisfy at least one objective:

- Explain

- Guide

- Confirm

- Orient

- Reveal

- Connect

Animations that exist only for visual decoration are prohibited.

---

## 3. Global Motion Rules

All motion shall be:

- Consistent

- Predictable

- Smooth

- Short

- Purposeful

- Performance-friendly

Motion should enhance usability rather than compete with content.

---

## 4. Motion Categories

### 4.1 Entrance Motion

Purpose:

Introduce new content.

Allowed:

- Fade In

- Slide Up

- Gentle Scale

- Opacity Transition

Avoid:

- Bounce

- Flip

- Rotate

- Zoom Explosion

---

### 4.2 Exit Motion

Purpose:

Gracefully remove content.

Allowed:

- Fade Out

- Slide Down

- Opacity Transition

---

### 4.3 Hover Motion

Purpose:

Indicate interactivity.

Allowed:

- Elevation

- Shadow adjustment

- Border emphasis

- Subtle scale (≤1.02)

- Background transition

Hover animations must never distract.

---

### 4.4 Focus Motion

Purpose:

Assist keyboard navigation.

Allowed:

- Focus ring transition

- Soft outline animation

- Color transition

Accessibility takes priority over aesthetics.

---

### 4.5 Scroll Motion

Purpose:

Progressive disclosure.

Allowed:

- Fade

- Slide

- Stagger

- Reveal

Animations should occur once unless interaction requires repetition.

---

### 4.6 State Change Motion

Purpose:

Communicate changes.

Examples:

- Loading

- Success

- Error

- Validation

- Expansion

- Collapse

Transitions should clarify what changed.

---

### 4.7 Layout Motion

Purpose:

Maintain continuity.

Examples:

- Accordion

- Tabs

- Dynamic content

- Filtering

- Sorting

Layout shifts should be animated subtly to preserve orientation.

---

## 5. Approved Motion Library

Only the following motion patterns are approved:

- Fade

- Fade Up

- Fade Down

- Slide Left

- Slide Right

- Slide Up

- Slide Down

- Stagger

- Scale (subtle)

- Opacity

- Height Expansion

- Width Expansion

- Cross Fade

No additional patterns may be introduced without architectural approval.

---

## 6. Motion Timing

Standard durations:

Motion

Duration

Instant

100ms

Fast

150ms

Standard

250ms

Slow

350ms

Large Transition

450ms

---

## 7. Motion Easing

Use a single easing family across the platform.

Recommended:

- Ease Out

- Ease In Out

Avoid:

- Bounce

- Elastic

- Overshoot

- Cartoon easing

Consistency builds familiarity.

---

## 8. Stagger Rules

Lists may stagger only when:

- Revealing multiple related items.

- Improving visual hierarchy.

- Preventing simultaneous visual overload.

Maximum delay between consecutive items:

50ms

Maximum recommended group size:

8 elements

---

## 9. Hero Motion

Hero sections may include:

- Fade

- Slide

- Progressive reveal

They shall never include:

- Auto-playing dramatic sequences

- Excessive parallax

- Continuous floating effects

- Particle systems

- Decorative background animations

The hero should emphasize the message, not the motion.

---

## 10. Navigation Motion

Navigation transitions should be nearly invisible.

Allowed:

- Menu expansion

- Dropdown fade

- Mega menu reveal

- Mobile drawer slide

Navigation should feel immediate and responsive.

---

## 11. Card Motion

Cards may animate on:

Hover:

- Elevation

- Shadow

- Border

- Subtle lift

Entrance:

- Fade

- Slide

Avoid exaggerated scaling or rotation.

---

## 12. Button Motion

Buttons may animate:

Hover:

- Background transition

- Border transition

- Shadow

- Slight elevation

Press:

- Brief compression (≤0.98 scale)

Buttons should feel responsive without drawing undue attention.

---

## 13. Page Transition Motion

Page transitions should be minimal.

Suggested:

- Cross Fade

- Fade + Slide

Avoid transitions that interrupt navigation flow.

---

## 14. Loading Motion

Loading states should communicate progress.

Preferred:

- Skeleton loaders

- Progress bars

- Subtle shimmer

Avoid spinning indicators for long-running operations where progress can be communicated more meaningfully.

---

## 15. Reduced Motion

The platform shall fully respect the user's operating system preference for reduced motion.

When enabled:

- Decorative motion is disabled.

- Transitions become instant or minimal.

- Essential feedback remains.

Accessibility requirements override motion preferences.

---

## 16. Performance Requirements

Motion shall maintain:

- 60 FPS where feasible.

- GPU-accelerated transforms.

- No layout thrashing.

- No unnecessary repainting.

- Minimal JavaScript involvement.

Prefer animating transform and opacity over layout properties.

---

## 17. Framer Motion Standards

Framer Motion is the approved animation library.

Guidelines:

- Centralize animation variants.

- Reuse motion definitions.

- Avoid inline animation objects.

- Name variants consistently.

- Keep animations declarative.

Example folders:

lib/motion/
│
├── variants.ts
├── transitions.ts
├── easing.ts
├── durations.ts
├── scroll.ts
└── index.ts

---

## 18. Motion Tokens

Motion should use centralized design tokens.

Examples:

Duration
---------
instant
fast
standard
slow

Easing
-------
easeOut
easeInOut

Distance
---------
small
medium
large

Opacity
--------
hidden
visible

Components should consume these tokens rather than defining custom values.

---

## 19. Motion Acceptance Criteria

Every animation must satisfy:

- Purposeful

- Consistent

- Accessible

- Smooth

- Responsive

- Performance optimized

- Reusable

- Centralized

- Respectful of reduced-motion settings

---

## 20. Final Motion Principle

> Motion should quietly reinforce confidence, never compete for attention.

A visitor should finish browsing AOVIAS with the impression that the interface felt polished, calm, and reliable—not because they noticed impressive animations, but because every transition helped them understand where they were, what changed, and what to do next.

---

### One addition I'd make

I'd introduce a small Motion Registry alongside your design tokens. Every approved animation would have a unique name (for example, fadeUp, sectionReveal, cardHover, menuOpen, heroIntro) with predefined variants, durations, easing, and usage guidelines. Then developers and autonomous AI agents would compose interfaces from this registry instead of inventing new animations, ensuring the entire website shares one coherent motion language.
