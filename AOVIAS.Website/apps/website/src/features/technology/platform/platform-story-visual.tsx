'use client';

import { MotionReveal, MotionStagger, MotionStaggerItem } from '@/lib/motion';
import { cn } from '@/lib/utils';

const stages = [
  {
    id: 'platform',
    label: 'AOVIAS Platform',
    detail: 'Shared foundation',
  },
  {
    id: 'services',
    label: 'Shared Platform Services',
    detail: 'Identity · Runtime · Intelligence · Integrations',
  },
  {
    id: 'products',
    label: 'Products',
    detail: 'Command · Studio · Future products',
  },
  {
    id: 'institutions',
    label: 'Institutions',
    detail: 'Hospitals · Government · Education · Enterprise',
  },
  {
    id: 'outcome',
    label: 'Stronger Institutions',
    detail: 'Technology as an instrument — not an end in itself',
  },
] as const;

export type PlatformStoryVisualProps = {
  className?: string;
};

/**
 * Flagship interactive platform story — scroll-revealed stages via Motion Registry.
 * Educates the platform → services → products → institutions → society chain.
 */
export function PlatformStoryVisual({ className }: PlatformStoryVisualProps) {
  return (
    <MotionReveal
      name="sectionReveal"
      className={cn(
        'border-border bg-card shadow-surface w-full rounded-lg border p-6 md:p-8',
        className,
      )}
    >
      <p className="text-muted-foreground mb-6 text-small font-medium tracking-wide uppercase">
        Platform story
      </p>
      <ol className="sr-only">
        {stages.map((stage) => (
          <li key={stage.id}>
            {stage.label}: {stage.detail}
          </li>
        ))}
      </ol>
      <MotionStagger className="flex flex-col gap-0" aria-hidden="true">
        {stages.map((stage, index) => (
          <MotionStaggerItem key={stage.id}>
            <div className="flex flex-col items-center">
              <div
                className={cn(
                  'border-border w-full max-w-xl rounded-md border px-4 py-3 text-center',
                  index === 0 && 'bg-primary/5 border-primary/30',
                  index === stages.length - 1 && 'bg-muted/60',
                  index !== 0 && index !== stages.length - 1 && 'bg-muted/30',
                )}
              >
                <p className="text-foreground text-small font-semibold">{stage.label}</p>
                <p className="text-muted-foreground mt-1 text-xs">{stage.detail}</p>
              </div>
              {index < stages.length - 1 ? (
                <div
                  className="text-muted-foreground flex h-8 items-center text-xs"
                  aria-hidden="true"
                >
                  ↓
                </div>
              ) : null}
            </div>
          </MotionStaggerItem>
        ))}
      </MotionStagger>
      <p className="text-muted-foreground mt-6 text-center text-small">
        From shared platform services to institutional outcomes — one coherent story.
      </p>
    </MotionReveal>
  );
}
