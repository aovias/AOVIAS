import type { ElementType, ReactNode } from 'react';

import { cn } from '@/lib/utils';

/**
 * Heading — semantic heading hierarchy consuming typography tokens.
 * Role aliases (page-title, hero-title, …) keep page titles visually identical.
 */
export type HeadingLevel = 1 | 2 | 3 | 4 | 5 | 6;
export type HeadingSize =
  | 'display-xl'
  | 'display-l'
  | 'display'
  | 'hero-title'
  | 'page-title'
  | 'section-title'
  | 'card-title'
  | 'subheading'
  | 'h1'
  | 'h2'
  | 'h3'
  | 'h4'
  | 'h5'
  | 'h6';

export type HeadingProps = {
  children: ReactNode;
  level?: HeadingLevel;
  /** Visual size override; defaults to match `level`. */
  size?: HeadingSize;
  className?: string;
  id?: string;
};

const levelTag: Record<HeadingLevel, ElementType> = {
  1: 'h1',
  2: 'h2',
  3: 'h3',
  4: 'h4',
  5: 'h5',
  6: 'h6',
};

const sizeFromLevel: Record<HeadingLevel, HeadingSize> = {
  1: 'page-title',
  2: 'section-title',
  3: 'h3',
  4: 'card-title',
  5: 'subheading',
  6: 'h6',
};

const sizeClass: Record<HeadingSize, string> = {
  'display-xl': 'text-display-xl font-semibold tracking-tight leading-tight',
  'display-l': 'text-display-l font-semibold tracking-tight leading-tight',
  display: 'text-display font-semibold tracking-tight leading-tight',
  'hero-title': 'text-hero-title font-semibold tracking-tight leading-tight',
  'page-title': 'text-page-title font-semibold tracking-tight leading-tight',
  'section-title': 'text-section-title font-semibold tracking-tight leading-snug',
  'card-title': 'text-card-title font-semibold leading-snug',
  subheading: 'text-subheading font-medium leading-normal',
  h1: 'text-page-title font-semibold tracking-tight leading-tight',
  h2: 'text-section-title font-semibold tracking-tight leading-snug',
  h3: 'text-h3 font-semibold leading-snug',
  h4: 'text-card-title font-semibold leading-snug',
  h5: 'text-subheading font-medium leading-normal',
  h6: 'text-h6 font-medium leading-normal',
};

export function Heading({
  children,
  level = 2,
  size,
  className,
  id,
}: HeadingProps) {
  const Tag = levelTag[level];
  const resolvedSize = size ?? sizeFromLevel[level];

  return (
    <Tag id={id} className={cn('text-ink', sizeClass[resolvedSize], className)}>
      {children}
    </Tag>
  );
}
