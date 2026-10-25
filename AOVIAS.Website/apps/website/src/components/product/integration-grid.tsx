import type { ReactNode } from 'react';

import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Grid,
  Link,
  Text,
  type GridCols,
} from '@/components/primitives';
import { cn } from '@/lib/utils';

export type IntegrationItem = {
  id: string;
  name: string;
  description?: string;
  category?: string;
  href?: string;
  /** Logo node or leave empty for text placeholder. */
  logo?: ReactNode;
};

/**
 * IntegrationGrid — partner/integration cards for future expansion.
 */
export type IntegrationGridProps = {
  items: readonly IntegrationItem[];
  cols?: Extract<GridCols, 2 | 3 | 4>;
  className?: string;
};

export function IntegrationGrid({
  items,
  cols = 3,
  className,
}: IntegrationGridProps) {
  return (
    <Grid cols={cols} gap={6} className={cn(className)}>
      {items.map((item) => (
        <Card key={item.id} variant={item.href ? 'interactive' : 'default'} className="h-full">
          <CardHeader>
            <div className="border-border bg-muted/40 mb-3 flex h-12 items-center justify-center rounded-md border px-3">
              {item.logo ? (
                <div className="opacity-80 grayscale" aria-hidden="true">
                  {item.logo}
                </div>
              ) : (
                <Text size="caption" tone="muted" weight="medium">
                  {item.name}
                </Text>
              )}
            </div>
            {item.category ? (
              <Text
                as="p"
                size="caption"
                tone="muted"
                weight="medium"
                className="mb-1 tracking-wide uppercase"
              >
                {item.category}
              </Text>
            ) : null}
            <CardTitle>{item.name}</CardTitle>
            {item.description ? (
              <CardDescription>{item.description}</CardDescription>
            ) : null}
          </CardHeader>
          {item.href ? (
            <CardContent className="pt-0">
              <Link href={item.href} tone="primary" className="text-small font-medium">
                View integration
              </Link>
            </CardContent>
          ) : null}
        </Card>
      ))}
    </Grid>
  );
}
