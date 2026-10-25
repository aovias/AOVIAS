import type { ReactNode } from 'react';

import { cn } from '@/lib/utils';

/**
 * Alert — foundation for inline status messaging (info/success/warning/error).
 */
export type AlertTone = 'info' | 'success' | 'warning' | 'error';

export type AlertProps = {
  children: ReactNode;
  title?: string;
  tone?: AlertTone;
  className?: string;
  /** Use `assertive` for errors that need immediate announcement. */
  live?: 'polite' | 'assertive' | 'off';
};

const toneClass: Record<AlertTone, string> = {
  // Subtle surfaces pair with semantic text (not *-foreground solid-fill tokens).
  info: 'border-info/30 bg-info-subtle text-info',
  success: 'border-success/30 bg-success-subtle text-success',
  warning: 'border-warning/30 bg-warning-subtle text-warning',
  error: 'border-error/30 bg-error-subtle text-error',
};

export function Alert({
  children,
  title,
  tone = 'info',
  className,
  live = 'polite',
}: AlertProps) {
  return (
    <div
      role="alert"
      aria-live={live === 'off' ? undefined : live}
      className={cn('rounded-md border px-4 py-3 text-small', toneClass[tone], className)}
    >
      {title ? <p className="mb-1 font-semibold">{title}</p> : null}
      <div className="leading-normal">{children}</div>
    </div>
  );
}
