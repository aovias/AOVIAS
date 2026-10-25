import type { ReactNode } from 'react';

import { Grid, type GridCols } from '@/components/primitives';
import { cn } from '@/lib/utils';

/**
 * FeatureGrid — responsive card grid with optional CSS stagger.
 * Pass FeatureCard / PrincipleCard / ValueCard children — no hardcoded items.
 * V11: CSS stagger only (Framer removed from this shared import path).
 */
export type FeatureGridProps = {
  children: ReactNode;
  cols?: Extract<GridCols, 2 | 3 | 4>;
  className?: string;
  /** Stagger children via CSS (respects prefers-reduced-motion). */
  stagger?: boolean;
};

export function FeatureGrid({
  children,
  cols = 3,
  className,
  stagger = true,
}: FeatureGridProps) {
  return (
    <Grid
      cols={cols}
      gap={6}
      className={cn(stagger && 'em-stagger-children', className)}
    >
      {children}
    </Grid>
  );
}
