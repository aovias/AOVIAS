import type { ReactNode } from 'react';

import { cn } from '@/lib/utils';

/**
 * Section — vertical page band with tokenized spacing rhythm.
 * Use for composing page structure, not marketing-specific sections.
 */
export type SectionSpacing = 'none' | 'sm' | 'md' | 'lg' | 'xl';

export type SectionProps = {
  children: ReactNode;
  className?: string;
  id?: string;
  spacing?: SectionSpacing;
  as?: 'section' | 'div';
};

const spacingClass: Record<SectionSpacing, string> = {
  none: '',
  sm: 'py-stack-xl',
  md: 'py-section-y-md',
  lg: 'py-section-y-lg',
  xl: 'py-section-y-xl',
};

export function Section({
  children,
  className,
  id,
  spacing = 'md',
  as: Tag = 'section',
}: SectionProps) {
  return (
    <Tag id={id} className={cn(spacingClass[spacing], className)}>
      {children}
    </Tag>
  );
}
