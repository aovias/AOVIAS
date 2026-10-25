'use client';

import type { ReactNode } from 'react';

import { MotionReveal } from '@/lib/motion';
import { cn } from '@/lib/utils';

export type MarketingRevealProps = {
  children: ReactNode;
  className?: string;
  /** When false, renders children without motion (SSR-identical wrapper). */
  enabled?: boolean;
};

/**
 * Optional scroll reveal using Motion Registry `sectionReveal`.
 * No inline animation objects.
 */
export function MarketingReveal({
  children,
  className,
  enabled = true,
}: MarketingRevealProps) {
  if (!enabled) {
    return <div className={className}>{children}</div>;
  }

  return (
    <MotionReveal name="sectionReveal" className={cn(className)}>
      {children}
    </MotionReveal>
  );
}
