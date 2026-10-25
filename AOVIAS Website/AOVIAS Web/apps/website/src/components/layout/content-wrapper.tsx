import type { ReactNode } from 'react';

import { cn } from '@/lib/utils';

export type ContentWrapperProps = {
  children: ReactNode;
  className?: string;
};

/** Inner content region for page body composition. */
export function ContentWrapper({ children, className }: ContentWrapperProps) {
  return <div className={cn('min-w-0 flex-1', className)}>{children}</div>;
}
