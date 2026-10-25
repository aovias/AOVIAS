/**
 * Motion durations (seconds) for Framer Motion.
 * Names align with `@aovias/design-system` Enterprise Motion Language (V10).
 */
export const motionDurations = {
  /** Immediate feedback ≈ 100ms */
  instant: 0.1,
  /** Immediate / micro ≈ 150ms */
  fast: 0.15,
  /** Hover refinement ≈ 200ms */
  hover: 0.2,
  /** Dropdown / UI ≈ 250ms */
  standard: 0.25,
  /** Mega-menu / dropdown ≈ 260ms */
  dropdown: 0.26,
  /** Slow UI ≈ 350ms */
  slow: 0.35,
  /** Overlay enter/exit ≈ 350ms */
  overlay: 0.35,
  /** Deliberate ≈ 450ms */
  deliberate: 0.45,
  /** Hero composed entrance ≈ 700ms */
  hero: 0.7,
  /** Diagram progression ≈ 800ms */
  diagram: 0.8,
  /** Horizon Reveal ceiling ≈ 800ms */
  signature: 0.8,
} as const;

export type MotionDurationToken = keyof typeof motionDurations;

/** Milliseconds — for CSS / documentation parity with design tokens. */
export const motionDurationsMs = {
  instant: 100,
  fast: 150,
  hover: 200,
  standard: 250,
  dropdown: 260,
  slow: 350,
  overlay: 350,
  deliberate: 450,
  hero: 700,
  diagram: 800,
  signature: 800,
} as const;

export const motionDelays = {
  none: 0,
  xs: 0.05,
  sm: 0.1,
  md: 0.15,
  lg: 0.2,
  /** Hero CTA sequencing */
  heroCta: 0.28,
  /** Diagram step stagger */
  diagramStep: 0.08,
} as const;

export type MotionDelayToken = keyof typeof motionDelays;
