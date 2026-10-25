import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Grid,
  Heading,
} from '@/components/primitives';
import { cn } from '@/lib/utils';

export type SpecRow = {
  id: string;
  label: string;
  value: string;
};

export type SpecCategory = {
  id: string;
  title: string;
  rows: readonly SpecRow[];
};

/**
 * TechnicalSpecificationBlock — categories of label/value specs.
 * Layout: responsive table (desktop) or stacked cards (mobile / card mode).
 */
export type TechnicalSpecificationBlockProps = {
  categories: readonly SpecCategory[];
  layout?: 'table' | 'cards' | 'auto';
  className?: string;
  id?: string;
};

export function TechnicalSpecificationBlock({
  categories,
  layout = 'auto',
  className,
  id,
}: TechnicalSpecificationBlockProps) {
  const useCards = layout === 'cards';

  return (
    <section id={id} className={cn('w-full py-section-y-md', className)}>
      {useCards ? (
        <Grid cols={categories.length === 1 ? 1 : 2} gap={6}>
          {categories.map((category) => (
            <Card key={category.id} variant="default">
              <CardHeader>
                <CardTitle>{category.title}</CardTitle>
              </CardHeader>
              <CardContent className="pt-0">
                <dl className="flex flex-col gap-3">
                  {category.rows.map((row) => (
                    <div
                      key={row.id}
                      className="border-border flex flex-col gap-1 border-b pb-3 last:border-0 last:pb-0 sm:flex-row sm:justify-between sm:gap-4"
                    >
                      <dt className="text-muted-foreground text-small">{row.label}</dt>
                      <dd className="text-foreground text-small font-medium sm:text-end">
                        {row.value}
                      </dd>
                    </div>
                  ))}
                </dl>
              </CardContent>
            </Card>
          ))}
        </Grid>
      ) : (
        <div className="flex flex-col gap-10">
          {categories.map((category) => (
            <div key={category.id}>
              <Heading level={3} size="h5" className="mb-4">
                {category.title}
              </Heading>
              <dl className="border-border divide-border flex flex-col divide-y rounded-lg border md:hidden">
                {category.rows.map((row) => (
                  <div key={row.id} className="flex flex-col gap-1 px-4 py-3">
                    <dt className="text-muted-foreground text-small">{row.label}</dt>
                    <dd className="text-foreground text-small font-medium">{row.value}</dd>
                  </div>
                ))}
              </dl>
              <div className="border-border hidden overflow-x-auto rounded-lg border md:block">
                <table className="text-small w-full text-left">
                  <caption className="sr-only">{category.title}</caption>
                  <thead className="bg-muted text-muted-foreground">
                    <tr>
                      <th scope="col" className="px-4 py-3 font-medium">
                        Specification
                      </th>
                      <th scope="col" className="px-4 py-3 font-medium">
                        Value
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    {category.rows.map((row) => (
                      <tr key={row.id} className="border-border border-t">
                        <th
                          scope="row"
                          className="text-muted-foreground px-4 py-3 font-normal"
                        >
                          {row.label}
                        </th>
                        <td className="text-foreground px-4 py-3 font-medium">
                          {row.value}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          ))}
        </div>
      )}
    </section>
  );
}
