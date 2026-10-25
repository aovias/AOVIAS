import type { InputHTMLAttributes } from 'react';

import { cn } from '@/lib/utils';

/**
 * Radio — native radio foundation. Group via shared `name`.
 */
export type RadioProps = Omit<InputHTMLAttributes<HTMLInputElement>, 'type'>;

export function Radio({ className, ...props }: RadioProps) {
  return (
    <input
      type="radio"
      className={cn(
        'border-input-border text-primary focus-visible:ring-focus-ring size-5 shrink-0 rounded-full border bg-input-background accent-primary focus-visible:ring-2 focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-50',
        className,
      )}
      {...props}
    />
  );
}
