import Link from 'next/link';
import type { ReactNode } from 'react';

import { MarketingReveal } from '@/components/marketing/reveal';
import { cn } from '@/lib/utils';

export type TechnologyIdentityTileProps = {
  title: string;
  description: string;
  href: string;
  linkLabel?: string;
  illustration: ReactNode;
  /** Technology domain for AVLS one-accent rule. */
  technology: 'platform' | 'ai' | 'engineering' | 'security';
  className?: string;
};

/**
 * Technology showcase tile — illustration + domain identity (AVLS §14).
 */
export function TechnologyIdentityTile({
  title,
  description,
  href,
  linkLabel = 'Explore',
  illustration,
  technology,
  className,
}: TechnologyIdentityTileProps) {
  return (
    <article
      data-technology={technology}
      className={cn(
        'border-border/70 bg-card/50 group relative flex h-full flex-col overflow-hidden rounded-xl border',
        'before:from-domain-accent before:via-domain-accent/40 before:absolute before:inset-x-0 before:top-0 before:h-0.5 before:bg-gradient-to-r before:to-transparent',
        'transition-[border-color,box-shadow] duration-[var(--duration-hover)] ease-[var(--easing-standard)] hover:border-domain-accent/35 hover:shadow-raised motion-reduce:transition-none',
        className,
      )}
    >
      <MarketingReveal className="border-border/50 border-b p-4 sm:p-5">
        {illustration}
      </MarketingReveal>
      <div className="flex flex-1 flex-col gap-2 p-5 sm:p-6">
        <p className="text-domain-accent text-caption font-semibold tracking-wide uppercase">
          {technology}
        </p>
        <h3 className="text-foreground text-body font-semibold tracking-tight">
          {title}
        </h3>
        <p className="text-muted-foreground text-small flex-1 leading-relaxed">
          {description}
        </p>
        <Link
          href={href}
          className="text-primary focus-visible:ring-focus-ring mt-3 inline-flex text-small font-medium underline-offset-4 hover:underline focus-visible:ring-2 focus-visible:outline-none"
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
