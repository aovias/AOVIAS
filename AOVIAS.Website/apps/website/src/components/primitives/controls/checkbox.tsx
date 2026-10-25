import type { InputHTMLAttributes } from 'react';

import { cn } from '@/lib/utils';

/**
 * Checkbox — native checkbox foundation with tokenized focus/checked styles.
 */
export type CheckboxProps = Omit<InputHTMLAttributes<HTMLInputElement>, 'type'>;

export function Checkbox({ className, ...props }: CheckboxProps) {
  return (
    <input
      type="checkbox"
      className={cn(
        'border-input-border text-primary focus-visible:ring-focus-ring size-5 shrink-0 rounded-sm border bg-input-background accent-primary focus-visible:ring-2 focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-50 aria-invalid:border-danger aria-invalid:ring-danger',
        className,
      )}
      {...props}
    />
  );
}
