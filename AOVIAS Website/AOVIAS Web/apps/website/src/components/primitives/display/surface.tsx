import type { ReactNode } from 'react';

import { cn } from '@/lib/utils';

/**
 * Surface — tonal background plane for grouping content.
 */
export type SurfaceTone = 'default' | 'muted' | 'subtle' | 'inverse';
export type SurfacePadding = 'none' | 'sm' | 'md' | 'lg';

export type SurfaceProps = {
  children: ReactNode;
  tone?: SurfaceTone;
  padding?: SurfacePadding;
  className?: string;
  as?: 'div' | 'section' | 'article' | 'aside';
};

const toneClass: Record<SurfaceTone, string> = {
  default: 'bg-card text-card-foreground',
  muted: 'bg-muted text-foreground',
  subtle: 'bg-subtle text-foreground',
  inverse: 'bg-foreground text-background',
};

const paddingClass: Record<SurfacePadding, string> = {
  none: '',
  sm: 'p-3',
  md: 'p-5',
  lg: 'p-8',
};

export function Surface({
  children,
  tone = 'default',
  padding = 'md',
  className,
  as: Tag = 'div',
}: SurfaceProps) {
  return (
    <Tag className={cn('rounded-lg', toneClass[tone], paddingClass[padding], className)}>
      {children}
    </Tag>
  );
}
