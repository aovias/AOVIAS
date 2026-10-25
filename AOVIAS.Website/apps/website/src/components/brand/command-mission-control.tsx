import { cn } from '@/lib/utils';

import { BrandShell, brandInk } from './brand-primitives';

export type CommandMissionControlProps = {
  title?: string;
  className?: string;
};

/**
 * High-fidelity Command mission-control surface — desirable product photography.
 * Real chrome: nav, metrics, incident table, timeline — not placeholder rectangles.
 */
export function CommandMissionControl({
  title = 'AOVIAS Command — Operations',
  className,
}: CommandMissionControlProps) {
  return (
    <BrandShell
      title={title}
      className={cn('overflow-hidden', className)}
      aspectClassName="min-h-[18rem] xl:min-h-[22rem]"
    >
      <div className="border-border bg-card text-foreground flex size-full min-h-[18rem] flex-col text-[0.65rem] leading-tight sm:text-[0.7rem]">
        {/* App chrome */}
        <div className="border-border bg-surface-muted/80 flex items-center gap-3 border-b px-3 py-2">
          <div className="flex gap-1" aria-hidden>
            <span className="bg-border size-2 rounded-full" />
            <span className="bg-border size-2 rounded-full" />
            <span className="bg-border size-2 rounded-full" />
          </div>
          <nav className="text-muted-foreground flex min-w-0 flex-1 items-center gap-3 overflow-hidden font-medium">
            <span className="text-foreground shrink-0">Operations</span>
            <span className="shrink-0">Incidents</span>
            <span className="hidden shrink-0 sm:inline">Queues</span>
            <span className="hidden shrink-0 md:inline">Approvals</span>
            <span className="hidden shrink-0 lg:inline">Observe</span>
          </nav>
          <span className="bg-primary/15 text-primary rounded px-1.5 py-0.5 text-[0.6rem] font-semibold tracking-wide uppercase">
            Live
          </span>
        </div>

        <div className="grid min-h-0 flex-1 grid-cols-12 gap-0">
          {/* Side rail */}
          <aside className="border-border bg-surface-muted/40 col-span-3 hidden border-e p-2 sm:block md:col-span-2">
            <p className="text-muted-foreground mb-2 px-1 text-[0.55rem] font-semibold tracking-wide uppercase">
              Queues
            </p>
            <ul className="space-y-1">
              {[
                { label: 'Priority', count: '12', active: true },
                { label: 'Investigate', count: '7', active: false },
                { label: 'Approvals', count: '4', active: false },
                { label: 'Deploy', count: '2', active: false },
              ].map((item) => (
                <li
                  key={item.label}
                  className={cn(
                    'flex items-center justify-between rounded-md px-1.5 py-1',
                    item.active && 'bg-primary/12 text-foreground',
                  )}
                >
                  <span className={item.active ? 'font-medium' : 'text-muted-foreground'}>
                    {item.label}
                  </span>
                  <span className="text-muted-foreground tabular-nums">{item.count}</span>
                </li>
              ))}
            </ul>
          </aside>

          {/* Main */}
          <div className="col-span-12 flex min-h-0 flex-col sm:col-span-9 md:col-span-10">
            {/* Metrics */}
            <div className="border-border grid grid-cols-2 gap-px border-b bg-border sm:grid-cols-4">
              {[
                { label: 'Open', value: '23', tone: 'default' },
                { label: 'MTTR', value: '18m', tone: 'default' },
                { label: 'SLA', value: '99.4%', tone: 'good' },
                { label: 'Pending', value: '4', tone: 'warn' },
              ].map((metric) => (
                <div
                  key={metric.label}
                  className="bg-card px-2.5 py-2 sm:px-3"
                >
                  <p className="text-muted-foreground text-[0.55rem] font-semibold tracking-wide uppercase">
                    {metric.label}
                  </p>
                  <p
                    className={cn(
                      'mt-0.5 text-sm font-semibold tabular-nums sm:text-base',
                      metric.tone === 'good' && 'text-primary',
                      metric.tone === 'warn' && 'text-amber-600 dark:text-amber-400',
                    )}
                  >
                    {metric.value}
                  </p>
                </div>
              ))}
            </div>

            {/* Table */}
            <div className="min-h-0 flex-1 overflow-hidden p-2 sm:p-3">
              <div className="border-border overflow-hidden rounded-lg border">
                <table className="w-full border-collapse text-left">
                  <thead className="bg-surface-muted/70 text-muted-foreground">
                    <tr>
                      <th className="px-2 py-1.5 font-semibold">Incident</th>
                      <th className="hidden px-2 py-1.5 font-semibold sm:table-cell">
                        Owner
                      </th>
                      <th className="px-2 py-1.5 font-semibold">State</th>
                      <th className="hidden px-2 py-1.5 font-semibold md:table-cell">
                        Age
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    {[
                      {
                        id: 'INC-1042',
                        title: 'Auth latency — Region West',
                        owner: 'Ops · M. Chen',
                        state: 'Investigate',
                        age: '14m',
                      },
                      {
                        id: 'INC-1041',
                        title: 'Queue backlog · Claims',
                        owner: 'Ops · A. Okonkwo',
                        state: 'Approve',
                        age: '31m',
                      },
                      {
                        id: 'INC-1039',
                        title: 'Deploy gate · Patch 2.4.1',
                        owner: 'Platform · J. Reyes',
                        state: 'Deploy',
                        age: '1h',
                      },
                      {
                        id: 'INC-1037',
                        title: 'Sensor drift · Facility 12',
                        owner: 'Ops · S. Patel',
                        state: 'Observe',
                        age: '2h',
                      },
                    ].map((row, index) => (
                      <tr
                        key={row.id}
                        className={cn(
                          'border-border border-t',
                          index === 0 && 'bg-primary/[0.06]',
                        )}
                      >
                        <td className="px-2 py-1.5">
                          <span className="text-foreground font-medium">{row.id}</span>
                          <span className="text-muted-foreground mt-0.5 block truncate max-w-[10rem] sm:max-w-[14rem]">
                            {row.title}
                          </span>
                        </td>
                        <td className="text-muted-foreground hidden px-2 py-1.5 sm:table-cell">
                          {row.owner}
                        </td>
                        <td className="px-2 py-1.5">
                          <span className="border-border bg-surface-muted inline-flex rounded border px-1.5 py-0.5 font-medium">
                            {row.state}
                          </span>
                        </td>
                        <td className="text-muted-foreground hidden px-2 py-1.5 tabular-nums md:table-cell">
                          {row.age}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              {/* Workflow strip */}
              <div className="mt-2 flex flex-wrap items-center gap-1.5 sm:gap-2">
                {['Detect', 'Investigate', 'Approve', 'Deploy', 'Observe'].map(
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
                          index === 1
                            ? 'border-primary/40 bg-primary/10 text-foreground'
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
      </div>
      {/* Keep brandInk referenced for theme consistency tooling */}
      <span className="sr-only">{brandInk.stroke}</span>
    </BrandShell>
  );
}
