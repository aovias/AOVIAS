import NextLink from 'next/link';
import type { ComponentProps, ReactNode } from 'react';

import { cn } from '@/lib/utils';

/**
 * Link — theme-aware text link with visible focus ring.
 * Uses Next.js Link for internal routes; set `external` for new-tab behavior.
 */
export type LinkProps = Omit<ComponentProps<typeof NextLink>, 'href'> & {
  href: string;
  children: ReactNode;
  external?: boolean;
  tone?: 'default' | 'muted' | 'primary';
  underline?: 'hover' | 'always' | 'none';
};

const toneClass = {
  default: 'text-ink hover:text-primary',
  muted: 'text-ink-muted hover:text-ink',
  primary: 'text-primary hover:text-primary-hover',
} as const;

const underlineClass = {
  hover: 'underline-offset-4 hover:underline',
  always: 'underline underline-offset-4',
  none: 'no-underline',
} as const;

export function Link({
  href,
  children,
  className,
  external = false,
  tone = 'primary',
  underline = 'hover',
  ...props
}: LinkProps) {
  return (
    <NextLink
      href={href}
      className={cn(
        'focus-visible:ring-ring rounded-sm transition-colors focus-visible:ring-2 focus-visible:outline-none',
        toneClass[tone],
        underlineClass[underline],
        className,
      )}
      {...(external
        ? { target: '_blank', rel: 'noreferrer' }
        : {})}
      {...props}
    >
      {children}
    </NextLink>
  );
}
