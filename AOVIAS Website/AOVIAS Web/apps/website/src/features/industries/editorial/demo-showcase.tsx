'use client';

import { Play } from 'lucide-react';
import { useState } from 'react';

import { Text } from '@/components/primitives';
import { cn } from '@/lib/utils';

export type DemoShowcaseProps = {
  /** Optional caption under the player (section description usually lives above). */
  caption?: string;
  runtimeLabel?: string;
  videoSrc?: string;
  posterSrc?: string;
  className?: string;
};

/**
 * Premium demo placeholder — no autoplay; lazy until interaction.
 */
export function DemoShowcase({
  caption,
  runtimeLabel = '3–5 minutes',
  videoSrc,
  posterSrc,
  className,
}: DemoShowcaseProps) {
  const [playing, setPlaying] = useState(false);

  return (
    <div className={cn('mx-auto w-full max-w-4xl', className)}>
      <div className="border-border bg-card/60 relative overflow-hidden rounded-2xl border shadow-[0_0_24px_rgba(74,114,173,0.18)]">
        <div className="from-domain-accent/15 via-transparent to-transparent absolute inset-0 bg-gradient-to-br" />
        {playing && videoSrc ? (
          <video
            className="aspect-video w-full bg-black object-cover"
            src={videoSrc}
            poster={posterSrc}
            controls
            autoPlay
            playsInline
          >
            <track kind="captions" />
          </video>
        ) : (
          <button
            type="button"
            className="group focus-visible:ring-ring relative flex aspect-video w-full items-center justify-center focus-visible:ring-2 focus-visible:outline-none"
            onClick={() => {
              if (videoSrc) setPlaying(true);
            }}
            aria-label={
              videoSrc
                ? 'Play product demonstration'
                : 'Product demonstration video coming soon'
            }
          >
            {posterSrc ? (
              // eslint-disable-next-line @next/next/no-img-element -- lazy placeholder poster
              <img
                src={posterSrc}
                alt=""
                className="absolute inset-0 size-full object-cover opacity-80"
                loading="lazy"
                decoding="async"
              />
            ) : (
              <div
                aria-hidden
                className="absolute inset-0 bg-[radial-gradient(ellipse_at_center,color-mix(in_srgb,var(--domain-accent)_18%,transparent),transparent_65%)]"
              />
            )}
            <span className="border-border/80 bg-background/80 text-foreground group-hover:border-domain-accent/50 group-hover:bg-background relative z-10 inline-flex size-16 items-center justify-center rounded-full border shadow-raised backdrop-blur-sm transition-colors sm:size-[4.5rem]">
              <Play className="text-domain-accent size-7 translate-x-0.5 fill-current sm:size-8" />
            </span>
            <span className="bg-background/70 text-muted-foreground absolute right-4 bottom-4 z-10 rounded-md px-2.5 py-1 text-caption font-medium backdrop-blur-sm">
              {runtimeLabel}
            </span>
          </button>
        )}
      </div>
      {caption ? (
        <Text
          as="p"
          size="body"
          tone="muted"
          className="mt-5 max-w-2xl leading-relaxed"
        >
          {caption}
        </Text>
      ) : null}
    </div>
  );
}
