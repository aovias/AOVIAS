import type { TextareaHTMLAttributes } from 'react';

import { cn } from '@/lib/utils';

/**
 * Textarea — multi-line text field foundation.
 */
export type TextareaProps = TextareaHTMLAttributes<HTMLTextAreaElement>;

export function Textarea({ className, ...props }: TextareaProps) {
  return (
    <textarea
      className={cn(
        'border-input-border bg-input-background text-ink placeholder:text-input-placeholder focus-visible:ring-focus-ring hover:border-primary/40 focus-visible:border-primary/50 shadow-surface flex min-h-24 w-full rounded-md border px-3 py-2 text-small transition-colors focus-visible:ring-2 focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-50 read-only:bg-muted read-only:text-ink-muted aria-invalid:border-danger aria-invalid:focus-visible:ring-danger',
        className,
      )}
      {...props}
    />
  );
}
