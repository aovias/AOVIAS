import type { ReactNode } from 'react';

import { cn } from '@/lib/utils';

/**
 * Quote — semantic blockquote with optional citation.
 */
export type QuoteProps = {
  children: ReactNode;
  cite?: string;
  attribution?: string;
  className?: string;
};

export function Quote({ children, cite, attribution, className }: QuoteProps) {
  return (
    <blockquote
      cite={cite}
      className={cn(
        'border-border text-ink border-l-2 pl-4 text-lead leading-relaxed italic',
        className,
      )}
    >
      {children}
      {attribution ? (
        <footer className="text-ink-muted mt-stack-sm text-small not-italic">
          — {attribution}
        </footer>
      ) : null}
    </blockquote>
  );
}
