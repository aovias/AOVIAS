import type { Variants } from 'framer-motion';

import { motionDistance, motionOpacity, motionScale } from './tokens';
import { motionTransitions } from './transitions';

/**
 * Centralized Framer Motion variants.
 * Components must consume these — do not define inline animation objects.
 */

export const fadeIn: Variants = {
  hidden: { opacity: motionOpacity.hidden },
  visible: {
    opacity: motionOpacity.visible,
    transition: motionTransitions.fade,
  },
  exit: {
    opacity: motionOpacity.hidden,
    transition: motionTransitions.exit,
  },
};

export const fadeOut: Variants = {
  hidden: { opacity: motionOpacity.visible },
  visible: {
    opacity: motionOpacity.hidden,
    transition: motionTransitions.fade,
  },
};

export const fadeUp: Variants = {
  hidden: { opacity: motionOpacity.hidden, y: motionDistance.md },
  visible: {
    opacity: motionOpacity.visible,
    y: motionDistance.none,
    transition: motionTransitions.reveal,
  },
  exit: {
    opacity: motionOpacity.hidden,
    y: motionDistance.sm,
    transition: motionTransitions.exit,
  },
};

export const fadeDown: Variants = {
  hidden: { opacity: motionOpacity.hidden, y: -motionDistance.md },
  visible: {
    opacity: motionOpacity.visible,
    y: motionDistance.none,
    transition: motionTransitions.reveal,
  },
  exit: {
    opacity: motionOpacity.hidden,
    y: -motionDistance.sm,
    transition: motionTransitions.exit,
  },
};

export const slideLeft: Variants = {
  hidden: { opacity: motionOpacity.hidden, x: motionDistance.lg },
  visible: {
    opacity: motionOpacity.visible,
    x: motionDistance.none,
    transition: motionTransitions.reveal,
  },
  exit: {
    opacity: motionOpacity.hidden,
    x: -motionDistance.sm,
    transition: motionTransitions.exit,
  },
};

export const slideRight: Variants = {
  hidden: { opacity: motionOpacity.hidden, x: -motionDistance.lg },
  visible: {
    opacity: motionOpacity.visible,
    x: motionDistance.none,
    transition: motionTransitions.reveal,
  },
  exit: {
    opacity: motionOpacity.hidden,
    x: motionDistance.sm,
    transition: motionTransitions.exit,
  },
};

export const scaleIn: Variants = {
  hidden: { opacity: motionOpacity.hidden, scale: motionScale.in },
  visible: {
    opacity: motionOpacity.visible,
    scale: motionScale.none,
    transition: motionTransitions.enter,
  },
  exit: {
    opacity: motionOpacity.hidden,
    scale: motionScale.in,
    transition: motionTransitions.exit,
  },
};

export const scaleOut: Variants = {
  hidden: { opacity: motionOpacity.visible, scale: motionScale.none },
  visible: {
    opacity: motionOpacity.hidden,
    scale: motionScale.out,
    transition: motionTransitions.exit,
  },
};

export const staggerContainer: Variants = {
  hidden: { opacity: motionOpacity.visible },
  visible: {
    opacity: motionOpacity.visible,
    transition: motionTransitions.staggerContainer,
  },
};

export const staggerItem: Variants = {
  hidden: { opacity: motionOpacity.hidden, y: motionDistance.sm },
  visible: {
    opacity: motionOpacity.visible,
    y: motionDistance.none,
    transition: motionTransitions.enter,
  },
};

/** Generic reveal — alias of fadeUp for section-level progressive disclosure. */
export const reveal: Variants = fadeUp;

export const pageTransition: Variants = {
  initial: { opacity: motionOpacity.hidden, y: motionDistance.sm },
  enter: {
    opacity: motionOpacity.visible,
    y: motionDistance.none,
    transition: motionTransitions.page,
  },
  exit: {
    opacity: motionOpacity.hidden,
    y: -motionDistance.xs,
    transition: motionTransitions.exit,
  },
};

/** Opacity-only variants for reduced-motion consumers. */
export const reducedFade: Variants = {
  hidden: { opacity: motionOpacity.hidden },
  visible: {
    opacity: motionOpacity.visible,
    transition: motionTransitions.reduced,
  },
  exit: {
    opacity: motionOpacity.hidden,
    transition: motionTransitions.reduced,
  },
};

export const motionVariants = {
  fadeIn,
  fadeOut,
  fadeUp,
  fadeDown,
  slideLeft,
  slideRight,
  scaleIn,
  scaleOut,
  staggerContainer,
  staggerItem,
  reveal,
  pageTransition,
  reducedFade,
} as const;

export type MotionVariantName = keyof typeof motionVariants;
