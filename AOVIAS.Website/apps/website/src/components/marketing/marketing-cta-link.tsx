import { ArrowRight, type LucideIcon } from 'lucide-react';
import Link from 'next/link';

import { buttonVariants, type ButtonProps } from '@/components/primitives';
import type { MarketingCta } from '@/components/marketing/types';
import { cn } from '@/lib/utils';

export type MarketingCtaLinkProps = MarketingCta & {
  variant?: ButtonProps['variant'];
  size?: ButtonProps['size'];
  className?: string;
  /** Trailing arrow for hero / promotional CTAs. */
  withArrow?: boolean;
  /** Optional leading icon (e.g. Play, Download, Calendar). */
  icon?: LucideIcon;
};

/** Anchor styled as a Button — keeps CTAs server-compatible. */
export function MarketingCtaLink({
  label,
  href,
  external = false,
  variant = 'primary',
  size = 'md',
  className,
  withArrow = false,
  icon: Icon,
}: MarketingCtaLinkProps) {
  return (
    <Link
      href={href}
      prefetch={false}
      className={cn(buttonVariants({ variant, size }), className)}
      {...(external ? { target: '_blank', rel: 'noreferrer' } : {})}
    >
      {Icon ? <Icon className="size-4" aria-hidden="true" /> : null}
      {label}
      {withArrow ? (
        <ArrowRight className="size-4" aria-hidden="true" />
      ) : null}
    </Link>
  );
}
