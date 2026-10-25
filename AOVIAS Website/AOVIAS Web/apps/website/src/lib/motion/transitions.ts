import type { Transition } from 'framer-motion';

import { motionDelays, motionDurations } from './durations';
import { motionEasing } from './easing';
import { motionStagger } from './tokens';

/**
 * Reusable Framer Motion transitions — Enterprise Motion Language (V10).
 * Prefer these over ad-hoc `transition` objects.
 */
export const motionTransitions = {
  instant: {
    duration: motionDurations.instant,
    ease: motionEasing.standard,
  },
  fast: {
    duration: motionDurations.fast,
    ease: motionEasing.standard,
  },
  standard: {
    duration: motionDurations.standard,
    ease: motionEasing.standard,
  },
  dropdown: {
    duration: motionDurations.dropdown,
    ease: motionEasing.enter,
  },
  slow: {
    duration: motionDurations.slow,
    ease: motionEasing.standard,
  },
  deliberate: {
    duration: motionDurations.deliberate,
    ease: motionEasing.standard,
  },
  enter: {
    duration: motionDurations.standard,
    ease: motionEasing.enter,
  },
  exit: {
    duration: motionDurations.fast,
    ease: motionEasing.exit,
  },
  /** Prefer for opacity-only fades. */
  fade: {
    duration: motionDurations.fast,
    ease: motionEasing.standard,
  },
  /** Prefer for transform + opacity entrances. */
  reveal: {
    duration: motionDurations.slow,
    ease: motionEasing.enter,
  },
  /** Hover/interactive micro-motion. */
  hover: {
    duration: motionDurations.hover,
    ease: motionEasing.standard,
  },
  /** Page-level route transition shell. */
  page: {
    duration: motionDurations.standard,
    ease: motionEasing.enter,
  },
  /** Hero composed entrance. */
  hero: {
    duration: motionDurations.hero,
    ease: motionEasing.enter,
  },
  /** Diagram layer / workflow progression. */
  diagram: {
    duration: motionDurations.diagram,
    ease: motionEasing.enter,
  },
  /** AVLS Horizon Reveal / signature moments (≤800ms). */
  signature: {
    duration: motionDurations.signature,
    ease: motionEasing.enter,
  },
  /** Search overlay / elevated panel enter. */
  overlay: {
    duration: motionDurations.overlay,
    ease: motionEasing.enter,
  },
  /** Overlay exit — slightly quicker for confidence. */
  overlayExit: {
    duration: motionDurations.slow,
    ease: motionEasing.exit,
  },
  /** Stagger children timing (container). */
  staggerContainer: {
    staggerChildren: motionStagger.default,
    delayChildren: motionDelays.xs,
  },
  staggerContainerTight: {
    staggerChildren: motionStagger.tight,
    delayChildren: motionDelays.none,
  },
  staggerContainerRelaxed: {
    staggerChildren: motionStagger.relaxed,
    delayChildren: motionDelays.sm,
  },
  staggerDiagram: {
    staggerChildren: motionDelays.diagramStep,
    delayChildren: motionDelays.sm,
  },
  /** Reduced-motion: keep essential feedback, drop travel. */
  reduced: {
    duration: 0.01,
    ease: motionEasing.linear,
  },
} as const satisfies Record<string, Transition>;

export type MotionTransitionName = keyof typeof motionTransitions;

export function getMotionTransition(name: MotionTransitionName): Transition {
  return motionTransitions[name];
}
