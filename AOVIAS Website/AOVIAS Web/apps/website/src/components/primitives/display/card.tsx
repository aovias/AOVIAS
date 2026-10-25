import type { HTMLAttributes, ReactNode } from 'react';

import { cn } from '@/lib/utils';

/**
 * Card — composable content container with variant elevations.
 * Slots: CardHeader, CardTitle, CardDescription, CardContent, CardFooter.
 */
export type CardVariant = 'default' | 'elevated' | 'flat' | 'interactive';

/**
 * Category accent for card personality (R1.VA.2).
 * Drives the top accent bar, subtle tint and icon tone via `--domain-accent`.
 */
export type CardAccent =
  | 'blue'
  | 'indigo'
  | 'cyan'
  | 'emerald'
  | 'slate'
  | 'purple'
  | 'premium';

export type CardProps = HTMLAttributes<HTMLDivElement> & {
  children: ReactNode;
  variant?: CardVariant;
  /** Category identity — sets accent color + a stronger top bar and subtle tint. */
  accent?: CardAccent;
};

const variantClass: Record<CardVariant, string> = {
  default:
    'border-border from-card to-surface-elevated/80 text-card-foreground border bg-gradient-to-b shadow-surface',
  elevated:
    'border-border from-card via-card to-primary-muted/25 text-card-foreground border bg-gradient-to-b shadow-raised',
  flat: 'border-border/70 bg-surface-muted text-ink border',
  interactive:
    'border-border from-card to-surface-elevated/90 text-card-foreground hover:border-primary/30 hover:shadow-raised focus-within:ring-focus-ring border bg-gradient-to-b shadow-surface transition-[colors,border-color,box-shadow,transform] duration-[var(--duration-hover)] ease-[var(--easing-standard)] hover:-translate-y-0.5 motion-reduce:transition-colors motion-reduce:hover:translate-y-0 focus-within:ring-2',
};

export function Card({
  children,
  className,
  variant = 'default',
  accent,
  ...props
}: CardProps) {
  return (
    <div
      data-accent={accent}
      className={cn(
        'relative overflow-hidden rounded-xl',
        // Base top hairline (inherits page/domain accent).
        'before:from-domain-accent/35 before:via-border before:absolute before:inset-x-0 before:top-0 before:h-px before:bg-gradient-to-r before:to-transparent',
        variantClass[variant],
        // Category personality: bolder top accent bar + faint corner tint.
        accent &&
          'before:from-domain-accent before:via-domain-accent/60 before:h-0.5 after:pointer-events-none after:absolute after:inset-0 after:-z-0 after:bg-gradient-to-br after:from-domain-accent/[0.07] after:via-transparent after:to-transparent',
        className,
      )}
      {...props}
    >
      {children}
    </div>
  );
}

export type CardHeaderProps = HTMLAttributes<HTMLDivElement> & { children: ReactNode };
export function CardHeader({ children, className, ...props }: CardHeaderProps) {
  return (
    <div className={cn('gap-stack-xs flex flex-col p-card pb-0', className)} {...props}>
      {children}
    </div>
  );
}

export type CardTitleProps = HTMLAttributes<HTMLHeadingElement> & { children: ReactNode };
export function CardTitle({ children, className, ...props }: CardTitleProps) {
  return (
    <h3
      className={cn('text-ink text-card-title font-semibold leading-snug', className)}
      {...props}
    >
      {children}
    </h3>
  );
}

export type CardDescriptionProps = HTMLAttributes<HTMLParagraphElement> & {
  children: ReactNode;
};
export function CardDescription({ children, className, ...props }: CardDescriptionProps) {
  return (
    <p className={cn('text-ink-muted text-small leading-normal', className)} {...props}>
      {children}
    </p>
  );
}

export type CardContentProps = HTMLAttributes<HTMLDivElement> & { children: ReactNode };
export function CardContent({ children, className, ...props }: CardContentProps) {
  return (
    <div className={cn('p-card', className)} {...props}>
      {children}
    </div>
  );
}

export type CardFooterProps = HTMLAttributes<HTMLDivElement> & { children: ReactNode };
export function CardFooter({ children, className, ...props }: CardFooterProps) {
  return (
    <div className={cn('gap-stack-md flex items-center p-card pt-0', className)} {...props}>
      {children}
    </div>
  );
}
