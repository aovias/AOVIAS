import type { InputHTMLAttributes } from 'react';

import { cn } from '@/lib/utils';

/**
 * Input — single-line text field foundation.
 */
export type InputProps = InputHTMLAttributes<HTMLInputElement>;

export function Input({ className, type = 'text', ...props }: InputProps) {
  return (
    <input
      type={type}
      className={cn(
        'border-input-border bg-input-background text-ink placeholder:text-input-placeholder focus-visible:ring-focus-ring hover:border-primary/40 focus-visible:border-primary/50 shadow-surface flex h-10 w-full rounded-md border px-3 py-1 text-small transition-colors duration-[var(--duration-hover)] ease-[var(--easing-standard)] focus-visible:ring-2 focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-50 read-only:bg-muted read-only:text-ink-muted aria-invalid:border-danger aria-invalid:focus-visible:ring-danger',
        className,
      )}
      {...props}
    />
  );
}
