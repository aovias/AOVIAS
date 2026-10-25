'use client';

import {
  motion,
  useReducedMotion,
  type HTMLMotionProps,
  type Variants,
} from 'framer-motion';
import type { ReactNode } from 'react';

import { motionConfig } from './config';
import { getFadeVariant, type FadeSystemName } from './fade';
import { withReducedMotion } from './reduced-motion';
import { getRegistryVariants, type MotionRegistryName } from './registry';
import { reveal as revealVariants } from './variants';

export type MotionRevealProps = Omit<HTMLMotionProps<'div'>, 'variants'> & {
  children: ReactNode;
  /**
   * Registry variant name (preferred) or omit for default `reveal`.
   * Hover registry names are not valid here.
   */
  name?: Extract<
    MotionRegistryName,
    | 'fadeIn'
    | 'fadeUp'
    | 'fadeDown'
    | 'slideLeft'
    | 'slideRight'
    | 'scaleIn'
    | 'reveal'
    | 'sectionReveal'
  >;
  /** Alternate: fade-system preset. */
  fade?: FadeSystemName;
  /** Replay when scrolling out/in. Default once from motionConfig. */
  once?: boolean;
  amount?: number | 'some' | 'all';
  className?: string;
};

function resolveRevealVariants(
  name: MotionRevealProps['name'],
  fade: FadeSystemName | undefined,
  prefersReduced: boolean | null,
): Variants {
  if (fade) return getFadeVariant(fade, Boolean(prefersReduced));
  if (name) return withReducedMotion(getRegistryVariants(name), prefersReduced);
  return withReducedMotion(revealVariants, prefersReduced);
}

/**
 * Scroll reveal — IntersectionObserver via Framer `whileInView`.
 * Performance-oriented: transform/opacity only; configurable once/replay.
 */
export function MotionReveal({
  children,
  name,
  fade,
  once = motionConfig.scrollReveal.once,
  amount = motionConfig.scrollReveal.amount,
  className,
  ...props
}: MotionRevealProps) {
  const prefersReduced = useReducedMotion();
  const variants = resolveRevealVariants(name, fade, prefersReduced);

  return (
    <motion.div
      className={className}
      variants={variants}
      initial="hidden"
      whileInView="visible"
      viewport={{ once, amount, margin: motionConfig.scrollReveal.margin }}
      {...props}
    >
      {children}
    </motion.div>
  );
}

export type UseScrollRevealOptions = {
  once?: boolean;
  amount?: number | 'some' | 'all';
  name?: MotionRevealProps['name'];
  fade?: FadeSystemName;
};

/**
 * Hook returning props to spread onto `motion.*` elements for scroll reveal.
 */
export function useScrollReveal(options: UseScrollRevealOptions = {}) {
  const prefersReduced = useReducedMotion();
  const {
    once = motionConfig.scrollReveal.once,
    amount = motionConfig.scrollReveal.amount,
    name,
    fade,
  } = options;

  return {
    variants: resolveRevealVariants(name, fade, prefersReduced),
    initial: 'hidden' as const,
    whileInView: 'visible' as const,
    viewport: {
      once,
      amount,
      margin: motionConfig.scrollReveal.margin,
    },
  };
}
