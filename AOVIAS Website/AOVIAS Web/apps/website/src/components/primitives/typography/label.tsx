import type { LabelHTMLAttributes, ReactNode } from 'react';

import { cn } from '@/lib/utils';

/**
 * Label — accessible form/control label.
 */
export type LabelProps = LabelHTMLAttributes<HTMLLabelElement> & {
  children: ReactNode;
  required?: boolean;
};

export function Label({ children, className, required, ...props }: LabelProps) {
  return (
    <label
      className={cn('text-foreground text-small font-medium leading-none', className)}
      {...props}
    >
      {children}
      {required ? (
        <span className="text-error ms-0.5" aria-hidden="true">
          *
        </span>
      ) : null}
    </label>
  );
}
