'use client';

import { MarketingReveal } from '@/components/marketing/reveal';
import { Heading, Text } from '@/components/primitives';
import { cn } from '@/lib/utils';

export type HealthcareVideoCardProps = {
  title: string;
  description: string;
  youtubeId: string;
  duration?: string;
  theme?: 'light' | 'dark';
  badge?: string;
  className?: string;
};

/**
 * Enterprise YouTube demo card — 16:9 embed, lazy-loaded, no autoplay.
 */
export function HealthcareVideoCard({
  title,
  description,
  youtubeId,
  duration,
  theme,
  badge,
  className,
}: HealthcareVideoCardProps) {
  const iframeTitle = `AOVIAS Command ${title}`;
  const embedSrc = `https://www.youtube.com/embed/${youtubeId}`;

  return (
    <MarketingReveal>
      <article
        className={cn(
          'border-border bg-card flex h-full flex-col overflow-hidden rounded-2xl border p-4 sm:p-6',
          'shadow-[0_0_24px_rgba(74,114,173,0.12)]',
          className,
        )}
        data-demo-theme={theme}
      >
        <div className="relative">
          {badge ? (
            <span className="bg-domain-accent text-background absolute top-3 right-3 z-10 rounded-md px-2 py-0.5 text-caption font-semibold tracking-wide uppercase">
              {badge}
            </span>
          ) : null}
          <div className="border-border bg-muted/40 relative aspect-video w-full overflow-hidden rounded-2xl border">
            <iframe
              title={iframeTitle}
              src={embedSrc}
              className="absolute inset-0 size-full"
              loading="lazy"
              allow="accelerometer; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share; fullscreen"
              allowFullScreen
              referrerPolicy="strict-origin-when-cross-origin"
            />
            {duration ? (
              <span className="bg-background/85 text-muted-foreground pointer-events-none absolute right-3 bottom-3 z-10 rounded-md px-2 py-1 text-caption font-medium backdrop-blur-sm">
                {duration}
              </span>
            ) : null}
          </div>
        </div>

        <div className="mt-5 flex min-w-0 flex-col gap-2">
          <Heading level={3} size="card-title" className="tracking-tight">
            {title}
          </Heading>
          <Text as="p" size="small" tone="muted" className="leading-relaxed">
            {description}
          </Text>
        </div>
      </article>
    </MarketingReveal>
  );
}
