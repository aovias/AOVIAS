import type { ReactNode } from 'react';

export type ShellTransitionProps = {
  children: ReactNode;
};

/**
 * Route shell wrapper (V11).
 * Framer page transitions removed from the critical path — mobile Lighthouse
 * LCP was gated on main-thread work from AnimatePresence on every marketing page.
 * Navigation still feels instantaneous; overlays keep their own motion.
 */
export function ShellTransition({ children }: ShellTransitionProps) {
  return <div className="flex min-h-0 flex-1 flex-col">{children}</div>;
}
