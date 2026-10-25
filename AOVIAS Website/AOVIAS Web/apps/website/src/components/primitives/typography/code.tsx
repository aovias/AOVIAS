import type { ReactNode } from 'react';

import { cn } from '@/lib/utils';

/**
 * Code — inline or block monospace text using mono token.
 */
export type CodeProps = {
  children: ReactNode;
  className?: string;
  block?: boolean;
};

export function Code({ children, className, block = false }: CodeProps) {
  if (block) {
    return (
      <pre
        className={cn(
          'border-border bg-muted text-foreground overflow-x-auto rounded-md border p-4 font-mono text-small leading-relaxed',
          className,
        )}
      >
        <code>{children}</code>
      </pre>
    );
  }

  return (
    <code
      className={cn(
        'bg-muted text-foreground rounded-sm px-1.5 py-0.5 font-mono text-[0.9em]',
        className,
      )}
    >
      {children}
    </code>
  );
}
