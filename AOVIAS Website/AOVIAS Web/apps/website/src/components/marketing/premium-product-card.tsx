import Link from 'next/link';
import type { ReactNode } from 'react';

import { cn } from '@/lib/utils';

export type PremiumProductCardProps = {
  title: string;
  description: string;
  href: string;
  linkLabel: string;
  media: ReactNode;
  className?: string;
  domainAttr?: {
    'data-product'?: string;
    'data-technology'?: string;
  };
};

/**
 * Premium product preview card — media + short copy + CTA.
 * Hover lift via CSS (V11 — no Framer on card import path).
 */
export function PremiumProductCard({
  title,
  description,
  href,
  linkLabel,
  media,
  className,
  domainAttr,
}: PremiumProductCardProps) {
  return (
    <article
      {...domainAttr}
      className={cn(
        'em-lift border-border/70 bg-card/60 group flex h-full flex-col overflow-hidden rounded-xl border',
        'before:from-domain-accent/55 before:via-border before:absolute before:inset-x-0 before:top-0 before:h-0.5 before:bg-gradient-to-r before:to-transparent',
        'relative transition-[border-color,box-shadow,transform] duration-[var(--duration-hover)] ease-[var(--easing-standard)] hover:border-domain-accent/30 hover:shadow-raised motion-reduce:transition-none',
        className,
      )}
    >
      <div className="border-border/50 bg-surface-muted/40 border-b p-3 sm:p-4">
        <div className="[&_[role=img]]:min-h-[7.5rem] [&_[role=img]]:rounded-lg">
          {media}
        </div>
      </div>
      <div className="flex flex-1 flex-col gap-2 p-5">
        <h3 className="text-foreground text-body font-semibold tracking-tight">
          {title}
        </h3>
        <p className="text-muted-foreground text-small flex-1 leading-relaxed">
          {description}
        </p>
        <Link
          href={href}
          className="text-primary focus-visible:ring-focus-ring mt-2 inline-flex text-small font-medium underline-offset-4 hover:underline focus-visible:ring-2 focus-visible:outline-none"
        >
          {linkLabel}
          <span aria-hidden="true" className="ms-1">
            →
          </span>
        </Link>
      </div>
    </article>
  );
}
