import type { ReactNode } from 'react';

import { cn } from '@/lib/utils';

/**
 * IconWrapper — consistent sizing/color/state wrapper for icons.
 * Decorative icons should set `decorative`; otherwise provide `label`.
 */
export type IconWrapperSize = 'sm' | 'md' | 'lg' | 'xl';
export type IconWrapperTone =
  | 'default'
  | 'muted'
  | 'primary'
  | 'domain'
  | 'success'
  | 'warning'
  | 'error';

export type IconWrapperProps = {
  children: ReactNode;
  size?: IconWrapperSize;
  tone?: IconWrapperTone;
  decorative?: boolean;
  label?: string;
  className?: string;
};

const sizeClass: Record<IconWrapperSize, string> = {
  sm: 'size-4 [&_svg]:size-3.5',
  md: 'size-5 [&_svg]:size-4',
  lg: 'size-6 [&_svg]:size-5',
  xl: 'size-8 [&_svg]:size-6',
};

const toneClass: Record<IconWrapperTone, string> = {
  default: 'text-ink',
  muted: 'text-ink-muted',
  primary: 'text-primary',
  /** Subtle product/tech/industry accent from DomainAccentScope */
  domain: 'text-domain-accent',
  success: 'text-success',
  warning: 'text-warning',
  error: 'text-danger',
};

export function IconWrapper({
  children,
  size = 'md',
  tone = 'default',
  decorative = true,
  label,
  className,
}: IconWrapperProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center justify-center',
        sizeClass[size],
        toneClass[tone],
        className,
      )}
      aria-hidden={decorative ? true : undefined}
      role={decorative ? undefined : 'img'}
      aria-label={decorative ? undefined : label}
    >
      {children}
    </span>
  );
}
