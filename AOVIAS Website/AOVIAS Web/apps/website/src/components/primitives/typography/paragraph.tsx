import type { ReactNode } from 'react';

import { cn } from '@/lib/utils';

/**
 * Paragraph — body copy with accessible line height.
 */
export type ParagraphProps = {
  children: ReactNode;
  size?: 'body-lg' | 'body' | 'small';
  tone?: 'default' | 'muted';
  className?: string;
};

const sizeClass = {
  'body-lg': 'text-body-lg',
  body: 'text-body',
  small: 'text-small',
} as const;

export function Paragraph({
  children,
  size = 'body',
  tone = 'default',
  className,
}: ParagraphProps) {
  return (
    <p
      className={cn(
        'leading-relaxed',
        sizeClass[size],
        tone === 'muted' ? 'text-ink-muted' : 'text-ink',
        className,
      )}
    >
      {children}
    </p>
  );
}
