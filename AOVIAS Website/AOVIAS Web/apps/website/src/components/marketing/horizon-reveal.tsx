import type { ReactNode } from 'react';

import { cn } from '@/lib/utils';

export type HorizonRevealProps = {
  children: ReactNode;
  className?: string;
  /** When false, render children without signature motion. */
  enabled?: boolean;
};

/**
 * AVLS Horizon Reveal (n10) — platinum 1px line draws via CSS.
 * Server component (no Framer) so the hero stays off the motion critical path.
 */
export function HorizonReveal({
  children,
  className,
  enabled = true,
}: HorizonRevealProps) {
  if (!enabled) {
    return <div className={className}>{children}</div>;
  }

  return (
    <div className={cn('relative', className)}>
      <div
        aria-hidden="true"
        className="em-horizon-line via-primary/55 pointer-events-none absolute inset-x-0 top-0 z-20 h-px bg-gradient-to-r from-transparent to-transparent"
      />
      {children}
    </div>
  );
}
