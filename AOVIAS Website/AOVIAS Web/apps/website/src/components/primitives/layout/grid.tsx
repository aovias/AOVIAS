import type { ReactNode } from 'react';

import { cn } from '@/lib/utils';

/**
 * Grid — responsive CSS grid with consistent gaps and alignment.
 * Safe to nest; columns collapse at smaller breakpoints.
 */
export type GridCols = 1 | 2 | 3 | 4 | 6 | 12;
export type GridGap = 2 | 3 | 4 | 5 | 6 | 8;
export type GridAlign = 'start' | 'center' | 'end' | 'stretch';
export type GridJustify = 'start' | 'center' | 'end' | 'between';

export type GridProps = {
  children: ReactNode;
  className?: string;
  cols?: GridCols;
  gap?: GridGap;
  align?: GridAlign;
  justify?: GridJustify;
};

const colsClass: Record<GridCols, string> = {
  1: 'grid-cols-1',
  2: 'grid-cols-1 sm:grid-cols-2',
  3: 'grid-cols-1 sm:grid-cols-2 lg:grid-cols-3',
  4: 'grid-cols-1 sm:grid-cols-2 lg:grid-cols-4',
  6: 'grid-cols-2 sm:grid-cols-3 lg:grid-cols-6',
  12: 'grid-cols-4 sm:grid-cols-6 lg:grid-cols-12',
};

const gapClass: Record<GridGap, string> = {
  2: 'gap-2',
  3: 'gap-3',
  4: 'gap-4 lg:gap-4',
  5: 'gap-5',
  6: 'gap-4 lg:gap-6',
  8: 'gap-6 lg:gap-8',
};

const alignClass: Record<GridAlign, string> = {
  start: 'items-start',
  center: 'items-center',
  end: 'items-end',
  stretch: 'items-stretch',
};

const justifyClass: Record<GridJustify, string> = {
  start: 'justify-items-start',
  center: 'justify-items-center',
  end: 'justify-items-end',
  between: 'justify-between',
};

export function Grid({
  children,
  className,
  cols = 12,
  gap = 6,
  align = 'stretch',
  justify = 'start',
}: GridProps) {
  return (
    <div
      className={cn(
        'grid',
        colsClass[cols],
        gapClass[gap],
        alignClass[align],
        justifyClass[justify],
        className,
      )}
    >
      {children}
    </div>
  );
}
