import { Stack } from '@/components/primitives';
import { cn } from '@/lib/utils';

export type ProductComparisonColumn = {
  id: string;
  name: string;
};

export type ProductComparisonRow = {
  id: string;
  label: string;
  values: Record<string, string>;
};

export type ProductComparisonTableProps = {
  columns: readonly ProductComparisonColumn[];
  rows: readonly ProductComparisonRow[];
  caption?: string;
  className?: string;
};

/**
 * Premium product comparison — purpose, users, capability, deployment, outcomes.
 * Text cells only; no marketing fluff.
 */
export function ProductComparisonTable({
  columns,
  rows,
  caption = 'AOVIAS product comparison',
  className,
}: ProductComparisonTableProps) {
  return (
    <div className={cn('w-full', className)}>
      {/* Desktop table */}
      <div className="border-border hidden overflow-hidden rounded-xl border md:block">
        <table className="w-full border-collapse text-left">
          <caption className="sr-only">{caption}</caption>
          <thead className="bg-surface-muted/80">
            <tr>
              <th
                scope="col"
                className="text-muted-foreground px-4 py-3 text-small font-semibold"
              >
                Dimension
              </th>
              {columns.map((column) => (
                <th
                  key={column.id}
                  scope="col"
                  className="text-foreground px-4 py-3 text-small font-semibold"
                >
                  {column.name}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => (
              <tr key={row.id} className="border-border border-t">
                <th
                  scope="row"
                  className="text-foreground px-4 py-3.5 align-top text-small font-semibold"
                >
                  {row.label}
                </th>
                {columns.map((column) => (
                  <td
                    key={column.id}
                    className="text-muted-foreground px-4 py-3.5 align-top text-small leading-relaxed"
                  >
                    {row.values[column.id] ?? '—'}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Mobile cards */}
      <Stack gap={4} className="md:hidden">
        {columns.map((column) => (
          <article
            key={column.id}
            className="border-border bg-card/50 overflow-hidden rounded-xl border"
          >
            <div className="border-border bg-surface-muted/60 border-b px-4 py-3">
              <p className="text-foreground text-small font-semibold">
                {column.name}
              </p>
            </div>
            <dl className="divide-border divide-y">
              {rows.map((row) => (
                <div key={row.id} className="px-4 py-3">
                  <dt className="text-muted-foreground text-[0.65rem] font-semibold tracking-wide uppercase">
                    {row.label}
                  </dt>
                  <dd className="text-foreground mt-1 text-small leading-relaxed">
                    {row.values[column.id] ?? '—'}
                  </dd>
                </div>
              ))}
            </dl>
          </article>
        ))}
      </Stack>
    </div>
  );
}
