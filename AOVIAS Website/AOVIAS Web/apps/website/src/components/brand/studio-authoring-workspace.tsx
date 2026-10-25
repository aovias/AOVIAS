import { cn } from '@/lib/utils';

import { BrandShell, brandInk } from './brand-primitives';

export type StudioAuthoringWorkspaceProps = {
  title?: string;
  className?: string;
};

/**
 * High-fidelity Studio authoring surface — editorial, structured, precise.
 */
export function StudioAuthoringWorkspace({
  title = 'AOVIAS Studio — Authoring',
  className,
}: StudioAuthoringWorkspaceProps) {
  return (
    <BrandShell
      title={title}
      className={cn('overflow-hidden', className)}
      aspectClassName="min-h-[18rem] xl:min-h-[22rem]"
    >
      <div className="border-border bg-card text-foreground flex size-full min-h-[18rem] flex-col text-[0.65rem] leading-tight sm:text-[0.7rem]">
        <div className="border-border bg-surface-muted/60 flex items-center gap-3 border-b px-3 py-2">
          <div className="flex gap-1" aria-hidden>
            <span className="bg-border size-2 rounded-full" />
            <span className="bg-border size-2 rounded-full" />
            <span className="bg-border size-2 rounded-full" />
          </div>
          <nav className="text-muted-foreground flex min-w-0 flex-1 items-center gap-3 font-medium">
            <span className="text-foreground">Library</span>
            <span>Models</span>
            <span className="hidden sm:inline">Standards</span>
            <span className="hidden md:inline">Publish</span>
          </nav>
          <span className="border-border rounded border px-1.5 py-0.5 text-[0.6rem] font-medium">
            v2.4 · Draft
          </span>
        </div>

        <div className="grid min-h-0 flex-1 grid-cols-12">
          <aside className="border-border col-span-4 hidden border-e p-2 sm:block md:col-span-3">
            <p className="text-muted-foreground mb-2 px-1 text-[0.55rem] font-semibold tracking-wide uppercase">
              Structure
            </p>
            <ul className="space-y-1">
              {['Overview', 'Data model', 'Controls', 'Publishing', 'Versions'].map(
                (item, index) => (
                  <li
                    key={item}
                    className={cn(
                      'rounded-md px-1.5 py-1',
                      index === 1
                        ? 'bg-domain-accent/15 text-foreground font-medium'
                        : 'text-muted-foreground',
                    )}
                  >
                    {item}
                  </li>
                ),
              )}
            </ul>
          </aside>

          <div className="col-span-12 flex flex-col p-2 sm:col-span-8 sm:p-3 md:col-span-9">
            <div className="mb-2 flex items-center justify-between gap-2">
              <div>
                <p className="text-muted-foreground text-[0.55rem] font-semibold tracking-wide uppercase">
                  Document
                </p>
                <p className="text-foreground text-sm font-semibold">
                  Institutional onboarding standard
                </p>
              </div>
              <div className="flex gap-1.5">
                <span className="border-border rounded border px-2 py-1 font-medium">
                  Review
                </span>
                <span className="bg-primary text-primary-foreground rounded px-2 py-1 font-medium">
                  Publish
                </span>
              </div>
            </div>

            <div className="border-border bg-surface-muted/30 flex-1 rounded-lg border p-3">
              <div className="bg-foreground/80 mb-2 h-2 w-2/3 rounded-sm opacity-80" />
              <div className="bg-foreground/40 mb-3 h-1.5 w-1/2 rounded-sm" />
              <div className="space-y-1.5">
                <div className="bg-foreground/25 h-1.5 w-full rounded-sm" />
                <div className="bg-foreground/25 h-1.5 w-[92%] rounded-sm" />
                <div className="bg-foreground/25 h-1.5 w-[85%] rounded-sm" />
                <div className="bg-foreground/20 mt-3 h-16 w-full rounded-md border border-dashed border-current/20" />
              </div>
            </div>

            <div className="mt-2 flex flex-wrap items-center gap-1.5 sm:gap-2">
              {['Model', 'Review', 'Publish', 'Version', 'Release'].map(
                (step, index) => (
                  <div key={step} className="flex items-center gap-1.5 sm:gap-2">
                    {index > 0 ? (
                      <span className="text-muted-foreground/50" aria-hidden>
                        →
                      </span>
                    ) : null}
                    <span
                      className={cn(
                        'rounded-md border px-1.5 py-0.5 font-medium',
                        index === 2
                          ? 'border-domain-accent/50 bg-domain-accent/15 text-foreground'
                          : 'border-border text-muted-foreground',
                      )}
                    >
                      {step}
                    </span>
                  </div>
                ),
              )}
            </div>
          </div>
        </div>
      </div>
      <span className="sr-only">{brandInk.stroke}</span>
    </BrandShell>
  );
}
