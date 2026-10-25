import type { Transition, Variants } from 'framer-motion';

import { motionTransitions } from './transitions';
import { motionVariants, reducedFade } from './variants';

/**
 * Reduced-motion helpers — accessibility takes precedence over aesthetics.
 */

/** Decorative travel (x/y/scale) collapses to opacity-only or instant. */
export function withReducedMotion(
  variants: Variants,
  prefersReducedMotion: boolean | null,
): Variants {
  if (!prefersReducedMotion) return variants;
  return reducedFade;
}

export function getSafeTransition(
  transition: Transition,
  prefersReducedMotion: boolean | null,
): Transition {
  if (!prefersReducedMotion) return transition;
  return motionTransitions.reduced;
}

/**
 * Resolve a named variant, substituting reducedFade when OS preference is set.
 */
export function resolveVariants(
  name: keyof typeof motionVariants,
  prefersReducedMotion: boolean | null,
): Variants {
  if (prefersReducedMotion) return reducedFade;
  return motionVariants[name];
}

/** True when decorative motion should be skipped entirely. */
export function shouldDisableDecorativeMotion(
  prefersReducedMotion: boolean | null,
): boolean {
  return Boolean(prefersReducedMotion);
}
