import type { ReactNode } from 'react';

import { cn } from '@/lib/utils';

export type MainContentProps = {
  children: ReactNode;
  className?: string;
};

export function MainContent({ children, className }: MainContentProps) {
  return (
    <main
      id="main-content"
      className={cn(
        'rounded-sm focus:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2',
        className,
      )}
      tabIndex={-1}
    >
      {children}
    </main>
  );
}
