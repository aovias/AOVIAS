import Link from 'next/link';
import type { ReactNode } from 'react';

import { cn } from '@/lib/utils';

export type CommercialProductCardProps = {
  name: string;
  purpose: string;
  audience: string;
  primaryValue: string;
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
 * Commercial portfolio card — artwork, purpose, audience, value, CTA.
 * Hover lift via CSS (V11 — no Framer on card import path).
 */
export function CommercialProductCard({
  name,
  purpose,
  audience,
  primaryValue,
  href,
  linkLabel,
  media,
  className,
  domainAttr,
}: CommercialProductCardProps) {
  return (
    <article
      {...domainAttr}
      className={cn(
        'em-lift border-border/70 bg-card/60 group relative flex h-full flex-col overflow-hidden rounded-xl border',
        'before:from-domain-accent/55 before:via-border before:absolute before:inset-x-0 before:top-0 before:h-0.5 before:bg-gradient-to-r before:to-transparent',
        'transition-[border-color,box-shadow,transform] duration-[var(--duration-hover)] ease-[var(--easing-standard)] hover:border-domain-accent/30 hover:shadow-raised motion-reduce:transition-none',
        className,
      )}
    >
      <div className="border-border/50 bg-surface-muted/40 border-b p-3 sm:p-4">
        <div className="[&_[role=img]]:min-h-[9rem] [&_[role=img]]:rounded-lg xl:[&_[role=img]]:min-h-[11rem]">
          {media}
        </div>
      </div>
      <div className="flex flex-1 flex-col gap-4 p-5 sm:p-6">
        <div>
          <h3 className="text-foreground text-body-lg font-semibold tracking-tight">
            {name}
          </h3>
          <p className="text-muted-foreground mt-2 text-small leading-relaxed">
            {purpose}
          </p>
        </div>
        <dl className="space-y-3">
          <div>
            <dt className="text-muted-foreground text-[0.65rem] font-semibold tracking-wide uppercase">
              Audience
            </dt>
            <dd className="text-foreground mt-1 text-small leading-relaxed">
              {audience}
            </dd>
          </div>
          <div>
            <dt className="text-muted-foreground text-[0.65rem] font-semibold tracking-wide uppercase">
              Primary value
            </dt>
            <dd className="text-foreground mt-1 text-small leading-relaxed">
              {primaryValue}
            </dd>
          </div>
        </dl>
        <Link
          href={href}
          className="text-primary focus-visible:ring-focus-ring mt-auto inline-flex pt-1 text-small font-medium underline-offset-4 hover:underline focus-visible:ring-2 focus-visible:outline-none"
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
