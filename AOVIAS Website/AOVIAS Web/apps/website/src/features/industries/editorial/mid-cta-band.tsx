import { MarketingCtaLink } from '@/components/marketing';
import type { IndustryMidCta } from '@/features/industries/editorial/types';
import { cn } from '@/lib/utils';

export type MidCtaBandProps = {
  cta: IndustryMidCta;
  className?: string;
};

/** Compact conversion strip between major content sections. */
export function MidCtaBand({ cta, className }: MidCtaBandProps) {
  return (
    <section
      aria-label={cta.title}
      className={cn(
        'border-border bg-surface-muted/80 relative w-full border-b py-10',
        className,
      )}
    >
      <div className="mx-auto flex max-w-4xl flex-col items-start gap-4 px-4 sm:flex-row sm:items-center sm:justify-between sm:px-6 lg:px-8">
        <div className="max-w-xl">
          <p className="text-foreground text-body font-semibold tracking-tight sm:text-body-lg">
            {cta.title}
          </p>
          {cta.description ? (
            <p className="text-muted-foreground mt-1 text-small leading-relaxed">
              {cta.description}
            </p>
          ) : null}
        </div>
        <div className="flex flex-wrap gap-3">
          <MarketingCtaLink
            {...cta.primaryCta}
            variant="primary"
            size="md"
            withArrow
          />
          {cta.secondaryCta ? (
            <MarketingCtaLink
              {...cta.secondaryCta}
              variant="soft"
              size="md"
              withArrow
            />
          ) : null}
        </div>
      </div>
    </section>
  );
}
