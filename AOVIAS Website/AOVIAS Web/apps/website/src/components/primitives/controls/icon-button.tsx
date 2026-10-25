import { cva, type VariantProps } from 'class-variance-authority';
import type { ButtonHTMLAttributes, ReactNode } from 'react';

import { Spinner } from '@/components/primitives/feedback/spinner';
import { cn } from '@/lib/utils';

/**
 * IconButton — square icon-only control. Always provide an accessible name.
 */
export const iconButtonVariants = cva(
  'inline-flex items-center justify-center rounded-md transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:pointer-events-none disabled:opacity-50 [&_svg]:size-4',
  {
    variants: {
      variant: {
        primary: 'bg-primary text-primary-foreground hover:bg-primary/90',
        secondary: 'bg-muted text-foreground hover:bg-muted/80',
        ghost: 'text-muted-foreground hover:bg-muted hover:text-foreground',
        outline: 'border-border bg-background hover:bg-muted border text-foreground',
        destructive: 'bg-error text-error-foreground hover:bg-error/90',
      },
      size: {
        sm: 'size-8',
        md: 'size-9',
        lg: 'size-11',
      },
    },
    defaultVariants: {
      variant: 'ghost',
      size: 'md',
    },
  },
);

export type IconButtonProps = ButtonHTMLAttributes<HTMLButtonElement> &
  VariantProps<typeof iconButtonVariants> & {
    /** Required accessible name for screen readers. */
    'aria-label': string;
    children: ReactNode;
    loading?: boolean;
  };

export function IconButton({
  className,
  variant = 'ghost',
  size = 'md',
  loading = false,
  disabled,
  children,
  type = 'button',
  ...props
}: IconButtonProps) {
  return (
    <button
      type={type}
      className={cn(iconButtonVariants({ variant, size }), className)}
      disabled={disabled || loading}
      aria-busy={loading || undefined}
      {...props}
    >
      {loading ? <Spinner size="sm" /> : children}
    </button>
  );
}
