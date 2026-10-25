import type { Easing } from 'framer-motion';

/**
 * Shared easing curves — calm enterprise motion (no bounce/elastic).
 * Mirrors design-system cubic-bezier tokens.
 */
export const motionEasing = {
  /** Standard: cubic-bezier(0.2, 0, 0, 1) */
  standard: [0.2, 0, 0, 1] as Easing,
  /** Enter: cubic-bezier(0.16, 1, 0.3, 1) */
  enter: [0.16, 1, 0.3, 1] as Easing,
  /** Exit: cubic-bezier(0.4, 0, 1, 1) */
  exit: [0.4, 0, 1, 1] as Easing,
  linear: 'linear' as Easing,
} as const;

export type MotionEasingToken = keyof typeof motionEasing;

/** CSS cubic-bezier strings for non-Framer consumers. */
export const motionEasingCss = {
  standard: 'cubic-bezier(0.2, 0, 0, 1)',
  enter: 'cubic-bezier(0.16, 1, 0.3, 1)',
  exit: 'cubic-bezier(0.4, 0, 1, 1)',
  linear: 'linear',
} as const;
