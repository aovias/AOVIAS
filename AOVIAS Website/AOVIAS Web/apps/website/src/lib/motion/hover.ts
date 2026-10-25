import type { TargetAndTransition, VariantLabels } from 'framer-motion';

import { motionOpacity, motionScale } from './tokens';
import { motionTransitions } from './transitions';

type HoverMotion = {
  whileHover: TargetAndTransition | VariantLabels;
  whileTap?: TargetAndTransition | VariantLabels;
  transition: typeof motionTransitions.hover;
};

/**
 * Subtle hover system — elevation/scale cues without playful bounce.
 * Prefer transform + opacity only.
 */
export const hoverMotions = {
  button: {
    whileHover: { scale: motionScale.hover },
    whileTap: { scale: motionScale.press },
    transition: motionTransitions.hover,
  },
  card: {
    whileHover: { y: -3, transition: motionTransitions.hover },
    whileTap: { y: 0 },
    transition: motionTransitions.hover,
  },
  link: {
    whileHover: { opacity: motionOpacity.soft },
    transition: motionTransitions.hover,
  },
  navItem: {
    whileHover: { opacity: motionOpacity.soft },
    transition: motionTransitions.hover,
  },
  interactive: {
    whileHover: { scale: motionScale.hover },
    whileTap: { scale: motionScale.press },
    transition: motionTransitions.hover,
  },
} as const satisfies Record<string, HoverMotion>;

export type HoverMotionName = keyof typeof hoverMotions;

export function getHoverMotion(name: HoverMotionName): HoverMotion {
  return hoverMotions[name];
}
