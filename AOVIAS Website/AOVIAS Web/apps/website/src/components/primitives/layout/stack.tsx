import type { ReactNode } from 'react';

import { cn } from '@/lib/utils';

/**
 * Stack — flex composition primitive (vertical/horizontal) with token gaps.
 */
export type StackGap = 1 | 2 | 3 | 4 | 5 | 6 | 8 | 10;
export type StackDirection = 'vertical' | 'horizontal';
export type StackAlign = 'start' | 'center' | 'end' | 'stretch' | 'baseline';
export type StackJustify = 'start' | 'center' | 'end' | 'between' | 'around';

export type StackProps = {
  children: ReactNode;
  className?: string;
  gap?: StackGap;
  direction?: StackDirection;
  align?: StackAlign;
  justify?: StackJustify;
  wrap?: boolean;
  as?: 'div' | 'ul' | 'ol' | 'fieldset';
};

const gapClass: Record<StackGap, string> = {
  1: 'gap-1',
  2: 'gap-2',
  3: 'gap-3',
  4: 'gap-4',
  5: 'gap-5',
  6: 'gap-6',
  8: 'gap-8',
  10: 'gap-10',
};

const alignClass: Record<StackAlign, string> = {
  start: 'items-start',
  center: 'items-center',
  end: 'items-end',
  stretch: 'items-stretch',
  baseline: 'items-baseline',
};

const justifyClass: Record<StackJustify, string> = {
  start: 'justify-start',
  center: 'justify-center',
  end: 'justify-end',
  between: 'justify-between',
  around: 'justify-around',
};

export function Stack({
  children,
  className,
  gap = 4,
  direction = 'vertical',
  align,
  justify = 'start',
  wrap = false,
  as: Tag = 'div',
}: StackProps) {
  const resolvedAlign =
    align ?? (direction === 'horizontal' ? 'center' : 'stretch');

  return (
    <Tag
      className={cn(
        'flex',
        direction === 'vertical' ? 'flex-col' : 'flex-row',
        wrap && 'flex-wrap',
        gapClass[gap],
        alignClass[resolvedAlign],
        justifyClass[justify],
        className,
      )}
    >
      {children}
    </Tag>
  );
}
