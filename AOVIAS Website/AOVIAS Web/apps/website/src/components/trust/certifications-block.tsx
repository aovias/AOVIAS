import type { Certification, CertificationStatus } from '@/components/trust/models';
import {
  Badge,
  Card,
  CardDescription,
  CardHeader,
  CardTitle,
  Grid,
  Link,
  Text,
  type GridCols,
} from '@/components/primitives';
import { cn } from '@/lib/utils';

/**
 * CertificationsBlock — certification cards from Certification models.
 */
export type CertificationsBlockProps = {
  items: readonly Certification[];
  cols?: Extract<GridCols, 2 | 3 | 4>;
  className?: string;
};

const statusLabel: Record<CertificationStatus, string> = {
  active: 'Active',
  'in-progress': 'In progress',
  planned: 'Planned',
};

const statusVariant: Record<
  CertificationStatus,
  'success' | 'warning' | 'secondary'
> = {
  active: 'success',
  'in-progress': 'warning',
  planned: 'secondary',
};

export function CertificationsBlock({
  items,
  cols = 3,
  className,
}: CertificationsBlockProps) {
  return (
    <Grid cols={cols} gap={6} className={cn(className)}>
      {items.map((item) => (
        <Card key={item.id} variant="default" className="h-full">
          <CardHeader>
            <div className="border-border bg-muted/40 mb-3 flex h-14 items-center justify-center rounded-md border px-3">
              {item.logo ? (
                <div className="opacity-90" aria-hidden="true">
                  {item.logo}
                </div>
              ) : (
                <Text size="caption" tone="muted" weight="medium">
                  {item.title}
                </Text>
              )}
            </div>
            <div className="mb-2 flex flex-wrap items-center gap-2">
              <CardTitle>{item.title}</CardTitle>
              {item.status ? (
                <Badge variant={statusVariant[item.status]}>
                  {statusLabel[item.status]}
                </Badge>
              ) : null}
            </div>
            {item.description ? (
              <CardDescription>{item.description}</CardDescription>
            ) : null}
            {item.href ? (
              <Link href={item.href} tone="primary" className="text-small mt-2 font-medium">
                View details
              </Link>
            ) : null}
          </CardHeader>
        </Card>
      ))}
    </Grid>
  );
}
