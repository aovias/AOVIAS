'use client';

import {
  motion,
  useReducedMotion,
  type HTMLMotionProps,
} from 'framer-motion';
import type { ReactNode } from 'react';

import { getFadeVariant, type FadeSystemName } from './fade';
import { getHoverMotion, type HoverMotionName } from './hover';
import { withReducedMotion } from './reduced-motion';
import { getRegistryVariants, type MotionRegistryName } from './registry';
import { staggerContainer, staggerItem } from './variants';

type VariantRegistryName = Extract<
  MotionRegistryName,
  | 'fadeIn'
  | 'fadeOut'
  | 'fadeUp'
  | 'fadeDown'
  | 'slideLeft'
  | 'slideRight'
  | 'scaleIn'
  | 'scaleOut'
  | 'reveal'
  | 'sectionReveal'
  | 'pageTransition'
>;

export type MotionFadeProps = Omit<HTMLMotionProps<'div'>, 'variants'> & {
  children: ReactNode;
  /** Fade system preset (section, card, dialog, …). */
  preset?: FadeSystemName;
  /** Or a registry variant name. */
  name?: VariantRegistryName;
  className?: string;
};

/**
 * Mount fade/entrance using centralized fade or registry variants.
 */
export function MotionFade({
  children,
  preset = 'component',
  name,
  className,
  ...props
}: MotionFadeProps) {
  const prefersReduced = useReducedMotion();
  const variants = name
    ? withReducedMotion(getRegistryVariants(name), prefersReduced)
    : getFadeVariant(preset, Boolean(prefersReduced));

  return (
    <motion.div
      className={className}
      variants={variants}
      initial="hidden"
      animate="visible"
      exit="exit"
      {...props}
    >
      {children}
    </motion.div>
  );
}

export type MotionHoverProps = Omit<
  HTMLMotionProps<'div'>,
  'whileHover' | 'whileTap' | 'transition'
> & {
  children: ReactNode;
  name?: HoverMotionName;
  className?: string;
  disabled?: boolean;
};

/**
 * Applies a named hover motion. Decorative hover disabled under reduced motion.
 */
export function MotionHover({
  children,
  name = 'interactive',
  className,
  disabled = false,
  ...props
}: MotionHoverProps) {
  const prefersReduced = useReducedMotion();
  const hover = getHoverMotion(name);

  if (disabled || prefersReduced) {
    return <div className={className}>{children}</div>;
  }

  return (
    <motion.div
      className={className}
      whileHover={hover.whileHover}
      {...(hover.whileTap ? { whileTap: hover.whileTap } : {})}
      transition={hover.transition}
      {...props}
    >
      {children}
    </motion.div>
  );
}

export type MotionStaggerProps = Omit<HTMLMotionProps<'div'>, 'variants'> & {
  children: ReactNode;
  className?: string;
};

export function MotionStagger({ children, className, ...props }: MotionStaggerProps) {
  const prefersReduced = useReducedMotion();
  const variants = withReducedMotion(staggerContainer, prefersReduced);

  return (
    <motion.div
      className={className}
      variants={variants}
      initial="hidden"
      whileInView="visible"
      viewport={{ once: true, amount: 0.2 }}
      {...props}
    >
      {children}
    </motion.div>
  );
}

export type MotionStaggerItemProps = Omit<HTMLMotionProps<'div'>, 'variants'> & {
  children: ReactNode;
  className?: string;
};

export function MotionStaggerItem({
  children,
  className,
  ...props
}: MotionStaggerItemProps) {
  const prefersReduced = useReducedMotion();
  const variants = withReducedMotion(staggerItem, prefersReduced);

  return (
    <motion.div className={className} variants={variants} {...props}>
      {children}
    </motion.div>
  );
}
