import { cn } from '@/lib/utils';

import {
  BrandGrid,
  BrandPanel,
  brandInk,
} from './brand-primitives';

export type WorkflowPanelProps = {
  title?: string;
  className?: string;
  steps?: readonly string[];
};

const defaultSteps = ['Observe', 'Decide', 'Act', 'Review'] as const;

/**
 * Calm process / operations visual for institutional workflows.
 */
export function WorkflowPanel({
  title = 'Operational workflow',
  className,
  steps = defaultSteps,
}: WorkflowPanelProps) {
  return (
    <div
      role="img"
      aria-label={title}
      className={cn(
        'border-border bg-card text-primary relative flex size-full min-h-[12rem] flex-col justify-center overflow-hidden rounded-lg border p-5',
        className,
      )}
    >
      <svg
        viewBox="0 0 320 40"
        className="pointer-events-none absolute inset-x-0 top-0 h-10 w-full opacity-60"
        aria-hidden
      >
        <BrandGrid width={320} height={40} step={16} opacity={0.08} />
        <BrandPanel x={0} y={0} width={320} height={40} opacity={brandInk.fillFaint} radius={0} />
      </svg>
      <p className="text-muted-foreground relative mb-4 text-xs font-semibold tracking-wide uppercase">
        {title}
      </p>
      <ol className="relative flex flex-wrap items-center gap-2 sm:gap-3">
        {steps.map((step, index) => (
          <li key={step} className="flex items-center gap-2 sm:gap-3">
            <span className="border-border bg-muted/60 text-foreground inline-flex items-center gap-2 rounded-md border px-3 py-2 text-small font-medium">
              <span
                className="bg-primary/15 text-primary inline-flex size-6 items-center justify-center rounded text-xs font-semibold"
                aria-hidden
              >
                {index + 1}
              </span>
              {step}
            </span>
            {index < steps.length - 1 ? (
              <span className="text-border hidden text-sm sm:inline" aria-hidden>
                →
              </span>
            ) : null}
          </li>
        ))}
      </ol>
      <div className="bg-muted/40 relative mt-5 h-2 w-full overflow-hidden rounded-full" aria-hidden>
        <div className="bg-primary/40 h-full w-2/3 rounded-full" />
      </div>
    </div>
  );
}
