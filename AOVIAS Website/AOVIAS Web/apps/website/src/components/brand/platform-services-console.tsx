import { cn } from '@/lib/utils';

import { BrandShell, brandInk } from './brand-primitives';

export type PlatformServicesConsoleProps = {
  title?: string;
  className?: string;
};

/**
 * High-fidelity Platform services surface — foundational, neutral, architectural.
 */
export function PlatformServicesConsole({
  title = 'AOVIAS Platform — Services',
  className,
}: PlatformServicesConsoleProps) {
  return (
    <BrandShell
      title={title}
      className={cn('overflow-hidden', className)}
      aspectClassName="min-h-[18rem] xl:min-h-[22rem]"
    >
      <div className="border-border bg-card text-foreground flex size-full min-h-[18rem] flex-col text-[0.65rem] leading-tight sm:text-[0.7rem]">
        <div className="border-border bg-surface-muted/70 flex items-center gap-3 border-b px-3 py-2">
          <div className="flex gap-1" aria-hidden>
            <span className="bg-border size-2 rounded-full" />
            <span className="bg-border size-2 rounded-full" />
            <span className="bg-border size-2 rounded-full" />
          </div>
          <nav className="text-muted-foreground flex min-w-0 flex-1 items-center gap-3 font-medium">
            <span className="text-foreground">Services</span>
            <span>Identity</span>
            <span className="hidden sm:inline">Policies</span>
            <span className="hidden md:inline">Deployments</span>
            <span className="hidden lg:inline">Observability</span>
          </nav>
          <span className="border-border rounded border px-1.5 py-0.5 text-[0.6rem] font-medium">
            Region · Multi
          </span>
        </div>

        <div className="grid min-h-0 flex-1 grid-cols-12">
          <aside className="border-border col-span-3 hidden border-e p-2 sm:block">
            <p className="text-muted-foreground mb-2 px-1 text-[0.55rem] font-semibold tracking-wide uppercase">
              Foundation
            </p>
            <ul className="space-y-1">
              {[
                { label: 'Identity', active: true },
                { label: 'Policies', active: false },
                { label: 'Services', active: false },
                { label: 'Deployments', active: false },
                { label: 'Observability', active: false },
              ].map((item) => (
                <li
                  key={item.label}
                  className={cn(
                    'rounded-md px-1.5 py-1',
                    item.active
                      ? 'bg-foreground/8 text-foreground font-medium'
                      : 'text-muted-foreground',
                  )}
                >
                  {item.label}
                </li>
              ))}
            </ul>
          </aside>

          <div className="col-span-12 flex flex-col p-2 sm:col-span-9 sm:p-3">
            <div className="mb-2 grid grid-cols-2 gap-2 sm:grid-cols-4">
              {[
                { label: 'Tenants', value: '48' },
                { label: 'Policies', value: '312' },
                { label: 'Services', value: '86' },
                { label: 'Health', value: '99.95%' },
              ].map((metric) => (
                <div
                  key={metric.label}
                  className="border-border bg-surface-muted/40 rounded-lg border px-2 py-1.5"
                >
                  <p className="text-muted-foreground text-[0.55rem] font-semibold tracking-wide uppercase">
                    {metric.label}
                  </p>
                  <p className="text-foreground mt-0.5 text-sm font-semibold tabular-nums">
                    {metric.value}
                  </p>
                </div>
              ))}
            </div>

            <div className="border-border min-h-0 flex-1 overflow-hidden rounded-lg border">
              <table className="w-full border-collapse text-left">
                <thead className="bg-surface-muted/70 text-muted-foreground">
                  <tr>
                    <th className="px-2 py-1.5 font-semibold">Capability</th>
                    <th className="hidden px-2 py-1.5 font-semibold sm:table-cell">
                      Scope
                    </th>
                    <th className="px-2 py-1.5 font-semibold">Status</th>
                  </tr>
                </thead>
                <tbody>
                  {[
                    {
                      name: 'Enterprise identity',
                      scope: 'Org · SSO',
                      status: 'Active',
                    },
                    {
                      name: 'Policy engine',
                      scope: 'Command · Studio',
                      status: 'Active',
                    },
                    {
                      name: 'Deployment fabric',
                      scope: 'Multi-region',
                      status: 'Active',
                    },
                    {
                      name: 'AI governance',
                      scope: 'Assisted workflows',
                      status: 'Governed',
                    },
                  ].map((row, index) => (
                    <tr
                      key={row.name}
                      className={cn(
                        'border-border border-t',
                        index === 0 && 'bg-foreground/[0.03]',
                      )}
                    >
                      <td className="text-foreground px-2 py-1.5 font-medium">
                        {row.name}
                      </td>
                      <td className="text-muted-foreground hidden px-2 py-1.5 sm:table-cell">
                        {row.scope}
                      </td>
                      <td className="px-2 py-1.5">
                        <span className="border-border bg-surface-muted inline-flex rounded border px-1.5 py-0.5 font-medium">
                          {row.status}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="mt-2 flex flex-wrap items-center gap-1.5 sm:gap-2">
              {[
                'Identity',
                'Policies',
                'Services',
                'Deployments',
                'Observability',
              ].map((step, index) => (
                <div key={step} className="flex items-center gap-1.5 sm:gap-2">
                  {index > 0 ? (
                    <span className="text-muted-foreground/50" aria-hidden>
                      →
                    </span>
                  ) : null}
                  <span
                    className={cn(
                      'rounded-md border px-1.5 py-0.5 font-medium',
                      index === 0
                        ? 'border-foreground/25 bg-foreground/5 text-foreground'
                        : 'border-border text-muted-foreground',
                    )}
                  >
                    {step}
                  </span>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
      <span className="sr-only">{brandInk.stroke}</span>
    </BrandShell>
  );
}
