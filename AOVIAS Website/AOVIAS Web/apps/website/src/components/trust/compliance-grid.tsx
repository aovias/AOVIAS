import type { ComplianceFramework } from '@/components/trust/models';
import {
  Card,
  CardDescription,
  CardHeader,
  CardTitle,
  Grid,
  IconWrapper,
  Link,
  type GridCols,
} from '@/components/primitives';
import { cn } from '@/lib/utils';

/**
 * ComplianceGrid — framework cards from ComplianceFramework models.
 */
export type ComplianceGridProps = {
  items: readonly ComplianceFramework[];
  cols?: Extract<GridCols, 2 | 3 | 4>;
  className?: string;
};

export function ComplianceGrid({
  items,
  cols = 3,
  className,
}: ComplianceGridProps) {
  return (
    <Grid cols={cols} gap={6} className={cn(className)}>
      {items.map((item) => (
        <Card key={item.id} variant={item.href ? 'interactive' : 'default'} className="h-full">
          <CardHeader>
            {item.icon ? (
              <IconWrapper tone="muted" size="lg" className="mb-2">
                {item.icon}
              </IconWrapper>
            ) : null}
            <CardTitle>{item.name}</CardTitle>
            <CardDescription>{item.description}</CardDescription>
            {item.href ? (
              <Link href={item.href} tone="primary" className="text-small mt-2 font-medium">
                Learn more
              </Link>
            ) : null}
          </CardHeader>
        </Card>
      ))}
    </Grid>
  );
}
