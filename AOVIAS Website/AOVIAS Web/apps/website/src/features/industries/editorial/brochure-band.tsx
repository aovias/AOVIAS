import { MarketingCtaLink } from '@/components/marketing';
import type { IndustryBrochure } from '@/features/industries/editorial/types';
import { cn } from '@/lib/utils';

export type BrochureBandProps = {
  brochure: IndustryBrochure;
  className?: string;
};

export function BrochureBand({ brochure, className }: BrochureBandProps) {
  return (
    <div
      className={cn(
        'border-border bg-card/60 grid items-center gap-8 rounded-2xl border p-6 sm:p-8 lg:grid-cols-[minmax(0,0.9fr)_minmax(0,1.1fr)] lg:gap-10',
        className,
      )}
    >
      <div className="border-border bg-surface-muted/80 mx-auto w-full max-w-[16rem] overflow-hidden rounded-lg border shadow-raised lg:mx-0">
        {/* eslint-disable-next-line @next/next/no-img-element -- lazy brochure cover placeholder */}
        <img
          src={brochure.coverSrc}
          alt={brochure.coverAlt}
          className="aspect-[3/4] w-full object-cover"
          loading="lazy"
          decoding="async"
        />
      </div>
      <div className="flex flex-col gap-4">
        <p className="text-muted-foreground text-body leading-relaxed">
          {brochure.description}
        </p>
        <MarketingCtaLink
          label={brochure.downloadCta.label}
          href={brochure.downloadCta.href}
          variant="soft"
          size="lg"
          withArrow
          className="w-fit"
        />
      </div>
    </div>
  );
}
