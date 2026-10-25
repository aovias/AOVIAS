import type { ReactNode } from 'react';

import { cn } from '@/lib/utils';

/**
 * Caption — supporting meta text beneath media or form fields.
 */
export type CaptionProps = {
  children: ReactNode;
  className?: string;
  as?: 'p' | 'span' | 'figcaption';
};

export function Caption({ children, className, as: Tag = 'p' }: CaptionProps) {
  return (
    <Tag className={cn('text-muted-foreground text-caption leading-normal', className)}>
      {children}
    </Tag>
  );
}
