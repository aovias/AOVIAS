---
title: "AOVIAS Enterprise Design Language"
source: appflowy
workspace: AOVIAS Website
space: General
view_id: "90e6abda-19f8-442f-a220-b6d01650c11c"
exported_at: "2026-07-19"
---

# AOVIAS Enterprise Design Language
### Purpose

This document establishes the visual language, interaction principles, layout standards, and design rules that govern every experience across the AOVIAS Public Website.

It is the visual expression of the AOVIAS Constitution and Brand Philosophy.

Every page, component, interaction, and animation shall adhere to these standards to ensure a consistent, trustworthy, and enterprise-grade experience.

---

## 1. Design Philosophy

Every design decision shall communicate:

- Trust

- Responsibility

- Engineering Excellence

- Clarity

- Confidence

- Stability

- Long-Term Thinking

The design language shall never prioritize novelty over usability.

Visitors should perceive AOVIAS as a mature institution rather than a young startup.

---

## 2. Visual Characteristics

The interface shall feel:

- Calm

- Spacious

- Elegant

- Structured

- Intelligent

- Professional

- Modern

- Accessible

It shall never feel:

- Loud

- Flashy

- Busy

- Trend-driven

- Over-designed

- Artificially futuristic

---

## 3. Layout Philosophy

Every page shall follow a predictable structure:

1. Clear introduction

2. Context

3. Core content

4. Supporting information

5. Trust signals

6. Call to action

Layouts shall guide attention naturally through hierarchy rather than visual tricks.

---

## 4. Grid System

Use a consistent 12-column responsive grid.

Maximum content width:

- 1280px for standard content.

- Up to 1440px for selected hero sections and comparison layouts.

Container behavior:

- Center aligned.

- Consistent horizontal padding.

- Balanced whitespace.

---

## 5. Spacing System

Adopt an 8-point spacing system.

Preferred spacing values:

4
8
12
16
24
32
40
48
64
80
96
128
160

Spacing shall be intentional and consistent.

Avoid arbitrary pixel values.

---

## 6. Typography

Typography is the primary design element.

Hierarchy shall rely on typography before color or decoration.

Example scale:

Element

Size

Display XL

72px

Display L

60px

H1

48px

H2

40px

H3

32px

H4

24px

H5

20px

H6

18px

Body Large

18px

Body

16px

Small

14px

Caption

12px

Line lengths should generally stay within 60–75 characters for readability.

---

## 7. Color Language

Color communicates meaning.

Primary brand colors shall be reserved for emphasis.

Use neutrals as the foundation.

Semantic colors:

- Success

- Warning

- Error

- Information

No decorative gradients unless explicitly approved.

Color should reinforce hierarchy, not compete for attention.

---

## 8. Border Radius

Use restrained rounding.

Suggested scale:

- Small: 6px

- Medium: 8px

- Large: 12px

- Extra Large: 16px

Avoid excessively rounded elements that suggest consumer or playful products.

---

## 9. Elevation

Elevation shall be subtle.

Levels:

- Flat

- Surface

- Raised

- Overlay

- Modal

Shadows should support depth without drawing attention to themselves.

---

## 10. Motion Language

Motion shall explain, not entertain.

Purposes include:

- Reinforcing hierarchy

- Guiding attention

- Providing feedback

- Supporting transitions

Avoid decorative animation.

---

## 11. Motion Timing

Suggested durations:

- Instant: 100ms

- Fast: 150ms

- Standard: 250ms

- Slow: 350ms

Maintain consistency across the site.

---

## 12. Motion Easing

Prefer smooth, natural easing curves.

Avoid exaggerated bounce, elastic, or playful effects.

Motion should feel composed and predictable.

---

## 13. Scroll Behavior

Scrolling should reveal information progressively.

Avoid parallax and excessive scroll-triggered effects.

Respect users who prefer reduced motion.

---

## 14. Iconography

Icons shall be:

- Simple

- Consistent

- Line-based

- Functional

Icons support comprehension; they should never replace clear text.

---

## 15. Imagery

Use imagery intentionally.

Preferred subjects:

- Professionals collaborating

- Institutions

- Healthcare environments

- Infrastructure

- Engineering

- Real-world impact

Avoid cliché stock imagery or unrealistic AI-generated scenes.

---

## 16. Illustration Style

Illustrations should be:

- Minimal

- Technical

- Abstract where appropriate

- Brand-aligned

They should clarify concepts rather than decorate empty space.

---

## 17. Cards

Cards shall present information clearly.

Guidelines:

- Consistent padding

- Clear hierarchy

- Minimal decoration

- Meaningful actions

- Predictable spacing

Cards should feel like structured information panels, not marketing tiles.

---

## 18. Buttons

Buttons shall communicate priority through hierarchy.

Primary actions should be limited on each screen.

Button styles should be consistent across all pages.

---

## 19. Forms

Forms should emphasize usability.

Requirements:

- Clear labels

- Helpful validation

- Logical grouping

- Generous spacing

- Accessible controls

Complex forms should be broken into manageable sections.

---

## 20. Navigation

Navigation should remain:

- Predictable

- Minimal

- Structured

- Consistent

Users should always know:

- Where they are

- What they can do next

- How to return

---

## 21. Responsive Breakpoints

Standard breakpoints:

- Mobile: 320–639px

- Small Tablet: 640–767px

- Tablet: 768–1023px

- Laptop: 1024–1279px

- Desktop: 1280–1535px

- Large Desktop: 1536px and above

Layouts should adapt fluidly between breakpoints rather than relying on abrupt visual changes.

---

## 22. Dark Mode

Dark mode is a first-class experience.

Requirements:

- Equal visual quality to light mode.

- Appropriate contrast.

- Brand consistency.

- Comfortable extended reading.

Do not invert colors mechanically.

---

## 23. Accessibility by Design

Accessibility shall influence every design decision.

Consider:

- Contrast

- Typography

- Focus

- Motion

- Hit targets

- Readability

Accessibility is not a post-design review.

---

## 24. Design Consistency

Every new component shall answer:

- Does it follow the spacing system?

- Does it use approved typography?

- Does it fit the color language?

- Does it respect motion rules?

- Does it align with the grid?

- Does it reinforce the AOVIAS brand?

If not, redesign it.

---

## 25. Final Design Principle

The visual language should never seek attention for itself.

Instead, it should quietly reinforce the perception that:

> AOVIAS is a thoughtful, engineering-led institution that builds technology with discipline, responsibility, and a long-term commitment to strengthening the organizations society depends upon.

---

### One enhancement I'd make

Since this will guide both designers and autonomous AI agents, I'd go one step further and create a Design Tokens Specification as a separate document (Document 04). It would translate these principles into concrete implementation values—such as Tailwind theme tokens, spacing variables, typography scales, color palettes, radii, shadows, and motion constants. That gives Cursor an exact vocabulary to implement, ensuring every generated component remains consistent across the entire website.
