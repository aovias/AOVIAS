import { cva, type VariantProps } from 'class-variance-authority';
import type { HTMLAttributes, ReactNode } from 'react';

import { cn } from '@/lib/utils';

/**
 * Badge — compact status/meta chip. Not interactive by default.
 */
export const badgeVariants = cva(
  'inline-flex items-center rounded-md px-2 py-0.5 text-caption font-medium',
  {
    variants: {
      variant: {
        default: 'bg-muted text-foreground border border-border/60',
        primary: 'bg-primary/12 text-primary border border-primary/25',
        secondary: 'bg-secondary-muted/70 text-foreground border border-secondary/25',
        outline: 'border-border text-foreground border',
        // Subtle surfaces use the semantic hue (not *-foreground, which is for solid fills).
        success: 'bg-success-subtle text-success',
        warning: 'bg-warning-subtle text-warning',
        error: 'bg-error-subtle text-error',
        info: 'bg-info-subtle text-info',
      },
    },
    defaultVariants: {
      variant: 'default',
    },
  },
);

export type BadgeProps = HTMLAttributes<HTMLSpanElement> &
  VariantProps<typeof badgeVariants> & {
    children: ReactNode;
  };

export function Badge({ className, variant = 'default', children, ...props }: BadgeProps) {
  return (
    <span className={cn(badgeVariants({ variant }), className)} {...props}>
      {children}
    </span>
  );
}
