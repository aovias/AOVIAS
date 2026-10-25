'use client';

import { MotionConfig } from 'framer-motion';
import type { ReactNode } from 'react';

import { motionConfig } from './config';

export type MotionProviderProps = {
  children: ReactNode;
};

/**
 * Global Framer Motion configuration.
 * `reducedMotion="user"` honors OS prefers-reduced-motion.
 */
export function MotionProvider({ children }: MotionProviderProps) {
  return (
    <MotionConfig reducedMotion={motionConfig.reducedMotion.strategy}>
      {children}
    </MotionConfig>
  );
}
