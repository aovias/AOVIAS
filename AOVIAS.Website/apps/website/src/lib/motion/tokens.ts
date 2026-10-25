/**
 * Centralized motion tokens — distance, opacity, scale, stagger, blur.
 * All animation values must originate here (or from durations/easing).
 */

export const motionDistance = {
  none: 0,
  xs: 4,
  sm: 8,
  md: 12,
  lg: 16,
  xl: 24,
} as const;

export type MotionDistanceToken = keyof typeof motionDistance;

export const motionOpacity = {
  hidden: 0,
  muted: 0.6,
  soft: 0.85,
  visible: 1,
} as const;

export type MotionOpacityToken = keyof typeof motionOpacity;

/** Subtle scale only — never playful overshoot. */
export const motionScale = {
  none: 1,
  in: 0.98,
  out: 1.02,
  hover: 1.015,
  press: 0.985,
} as const;

export type MotionScaleToken = keyof typeof motionScale;

/** Minimal rotation — reserved; prefer unused for public UI. */
export const motionRotation = {
  none: 0,
  subtle: 0.5,
} as const;

export type MotionRotationToken = keyof typeof motionRotation;

export const motionStagger = {
  none: 0,
  tight: 0.04,
  default: 0.06,
  relaxed: 0.1,
  /** Cap from design-system `staggerMax` (50ms). */
  max: 0.05,
} as const;

export type MotionStaggerToken = keyof typeof motionStagger;

export const motionBlur = {
  none: '0px',
  soft: '2px',
} as const;

export type MotionBlurToken = keyof typeof motionBlur;

export const motionTokens = {
  distance: motionDistance,
  opacity: motionOpacity,
  scale: motionScale,
  rotation: motionRotation,
  stagger: motionStagger,
  blur: motionBlur,
} as const;
