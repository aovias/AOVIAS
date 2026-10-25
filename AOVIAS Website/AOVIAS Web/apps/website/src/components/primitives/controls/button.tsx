import { cva, type VariantProps } from 'class-variance-authority';
import type { ButtonHTMLAttributes, ReactNode } from 'react';

import { Spinner } from '@/components/primitives/feedback/spinner';
import { cn } from '@/lib/utils';

/**
 * Button — primary interactive control.
 *
 * Variants: primary | secondary | ghost | outline | destructive | link
 * Sizes: sm | md | lg
 * States: default, hover, focus-visible, disabled, loading
 */
export const buttonVariants = cva(
  'inline-flex items-center justify-center gap-2 rounded-md text-small font-medium transition-[color,background-color,box-shadow,transform] duration-[var(--duration-hover)] ease-[var(--easing-standard)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:pointer-events-none disabled:opacity-50 motion-reduce:transition-colors [&_svg]:pointer-events-none [&_svg]:shrink-0',
  {
    variants: {
      variant: {
        primary:
          'bg-primary text-primary-foreground shadow-surface hover:bg-primary-hover hover:shadow-raised active:scale-[0.985] motion-reduce:active:scale-100',
        secondary:
          'bg-secondary-muted text-ink hover:bg-muted active:scale-[0.985] motion-reduce:active:scale-100',
        ghost: 'hover:bg-muted text-ink',
        outline:
          'border-border bg-surface-elevated hover:bg-muted border text-ink shadow-surface hover:shadow-raised active:scale-[0.985] motion-reduce:active:scale-100',
        /** Transparent soft-blue border — marketing ghost outline (About / Explore). */
        soft:
          'border-primary/75 text-primary hover:bg-primary/10 rounded-lg border bg-transparent shadow-none active:scale-[0.985] motion-reduce:active:scale-100',
        destructive:
          'bg-danger text-danger-foreground hover:bg-danger/90',
        success:
          'bg-success text-success-foreground hover:bg-success/90',
        link: 'text-primary h-auto rounded-sm px-0 underline-offset-4 hover:underline',
      },
      size: {
        sm: 'h-8 px-3 text-caption',
        md: 'h-9 px-4',
        lg: 'h-11 px-6 text-body',
      },
    },
    defaultVariants: {
      variant: 'primary',
      size: 'md',
    },
  },
);

export type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> &
  VariantProps<typeof buttonVariants> & {
    loading?: boolean;
    children: ReactNode;
  };

export function Button({
  className,
  variant = 'primary',
  size = 'md',
  loading = false,
  disabled,
  children,
  type = 'button',
  ...props
}: ButtonProps) {
  const isDisabled = disabled || loading;

  return (
    <button
      type={type}
      className={cn(buttonVariants({ variant, size }), className)}
      disabled={isDisabled}
      aria-busy={loading || undefined}
      {...props}
    >
      {loading ? <Spinner size="sm" label="Loading" /> : null}
      {children}
    </button>
  );
}
