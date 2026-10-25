import Link from 'next/link';
import type { ReactNode } from 'react';

import { MarketingReveal } from '@/components/marketing/reveal';
import { cn } from '@/lib/utils';

export type VisualStoryPanelProps = {
  title: string;
  description: string;
  href?: string;
  linkLabel?: string;
  illustration: ReactNode;
  /** Domain identity data attribute for one accent per panel. */
  domainAttr?: {
    'data-product'?: string;
    'data-technology'?: string;
    'data-industry'?: string;
  };
  className?: string;
  /** Reverse text/visual on large screens. */
  reverse?: boolean;
};

/**
 * Illustration-led story panel — replaces Lucide FeatureCards on the North Star homepage.
 */
export function VisualStoryPanel({
  title,
  description,
  href,
  linkLabel = 'Learn more',
  illustration,
  domainAttr,
  className,
  reverse = false,
}: VisualStoryPanelProps) {
  return (
    <div
      {...domainAttr}
      className={cn(
        'border-border/60 bg-surface-elevated/40 relative overflow-hidden rounded-xl border',
        'before:from-domain-accent/50 before:via-border before:absolute before:inset-x-0 before:top-0 before:h-0.5 before:bg-gradient-to-r before:to-transparent',
        className,
      )}
    >
      <div
        className={cn(
          'grid items-center gap-6 p-5 sm:p-6 lg:gap-8 lg:p-7',
          'lg:grid-cols-2',
          reverse && 'lg:[&>*:first-child]:order-2',
        )}
      >
        <div className="min-w-0">
          <MarketingReveal>
            <h3 className="text-foreground text-subheading font-semibold tracking-tight">
              {title}
            </h3>
            <p className="text-muted-foreground mt-2 text-small leading-relaxed">
              {description}
            </p>
            {href ? (
              <Link
                href={href}
                className="text-primary focus-visible:ring-focus-ring mt-4 inline-flex text-small font-medium underline-offset-4 hover:underline focus-visible:ring-2 focus-visible:outline-none"
              >
                {linkLabel}
                <span aria-hidden="true" className="ms-1">
                  →
                </span>
              </Link>
            ) : null}
          </MarketingReveal>
        </div>
        <MarketingReveal className="min-w-0">{illustration}</MarketingReveal>
      </div>
    </div>
  );
}
