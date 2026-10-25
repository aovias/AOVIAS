import type { Transition, Variants } from 'framer-motion';

import { getFadeVariant, type FadeSystemName } from './fade';
import { getHoverMotion, type HoverMotionName } from './hover';
import { withReducedMotion } from './reduced-motion';
import {
  getRegistryVariants,
  type MotionRegistryName,
} from './registry';
import { getMotionTransition, type MotionTransitionName } from './transitions';
import { motionVariants, type MotionVariantName } from './variants';

/**
 * Animation utilities — single entry helpers for consuming the motion system.
 */

export function createStaggerContainer(
  transitionName: Extract<
    MotionTransitionName,
    'staggerContainer' | 'staggerContainerTight' | 'staggerContainerRelaxed'
  > = 'staggerContainer',
): Variants {
  return {
    hidden: { opacity: 1 },
    visible: {
      opacity: 1,
      transition: getMotionTransition(transitionName),
    },
  };
}

export function getNamedVariants(
  name: MotionVariantName,
  prefersReducedMotion: boolean | null = false,
): Variants {
  return withReducedMotion(motionVariants[name], prefersReducedMotion);
}

export function getNamedRegistryVariants(
  name: Extract<
    MotionRegistryName,
    | 'fadeIn'
    | 'fadeOut'
    | 'fadeUp'
    | 'fadeDown'
    | 'slideLeft'
    | 'slideRight'
    | 'scaleIn'
    | 'scaleOut'
    | 'staggerContainer'
    | 'staggerItem'
    | 'reveal'
    | 'sectionReveal'
    | 'pageTransition'
  >,
  prefersReducedMotion: boolean | null = false,
): Variants {
  return withReducedMotion(getRegistryVariants(name), prefersReducedMotion);
}

export function getNamedFade(
  name: FadeSystemName,
  prefersReducedMotion = false,
): Variants {
  return getFadeVariant(name, prefersReducedMotion);
}

export function getNamedHover(name: HoverMotionName) {
  return getHoverMotion(name);
}

export function getNamedTransition(name: MotionTransitionName): Transition {
  return getMotionTransition(name);
}

/** Spread-ready props for a simple mount animation. */
export function getEnterMotionProps(
  name: MotionVariantName = 'fadeUp',
  prefersReducedMotion: boolean | null = false,
) {
  return {
    variants: getNamedVariants(name, prefersReducedMotion),
    initial: 'hidden' as const,
    animate: 'visible' as const,
    exit: 'exit' as const,
  };
}
