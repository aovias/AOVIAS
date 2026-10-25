import type { SelectHTMLAttributes } from 'react';

import { cn } from '@/lib/utils';

/**
 * Select — native select foundation (no portal/menu library).
 * Suitable for progressive enhancement; advanced menus arrive later.
 */
export type SelectProps = SelectHTMLAttributes<HTMLSelectElement>;

export function Select({ className, children, ...props }: SelectProps) {
  return (
    <select
      className={cn(
        'border-input-border bg-input-background text-ink focus-visible:ring-focus-ring hover:border-primary/40 focus-visible:border-primary/50 shadow-surface flex h-10 w-full rounded-md border px-3 py-1 text-small transition-colors focus-visible:ring-2 focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-50 aria-invalid:border-danger aria-invalid:focus-visible:ring-danger',
        className,
      )}
      {...props}
    >
      {children}
    </select>
  );
}
