import type { ReactNode } from 'react';

import {
  Card,
  CardDescription,
  CardHeader,
  CardTitle,
  Grid,
  IconWrapper,
  Text,
  type GridCols,
} from '@/components/primitives';
import { MotionStagger, MotionStaggerItem } from '@/lib/motion';
import { cn } from '@/lib/utils';

export type CapabilityItem = {
  id: string;
  title: string;
  description: string;
  icon?: ReactNode;
  category?: string;
};

/**
 * CapabilityGrid — categorized capability cards.
 */
export type CapabilityGridProps = {
  items: readonly CapabilityItem[];
  cols?: Extract<GridCols, 2 | 3 | 4>;
  className?: string;
  stagger?: boolean;
  /** Group visually by category label when present. */
  showCategories?: boolean;
};

export function CapabilityGrid({
  items,
  cols = 3,
  className,
  stagger = true,
  showCategories = true,
}: CapabilityGridProps) {
  const renderCard = (item: CapabilityItem) => (
    <Card variant="default" className="h-full">
      <CardHeader>
        {item.icon ? (
          <IconWrapper tone="domain" size="lg" className="mb-2">
            {item.icon}
          </IconWrapper>
        ) : null}
        {showCategories && item.category ? (
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
        <CardTitle>{item.title}</CardTitle>
        <CardDescription>{item.description}</CardDescription>
      </CardHeader>
    </Card>
  );

  if (!stagger) {
    return (
      <Grid cols={cols} gap={6} className={cn(className)}>
        {items.map((item) => (
          <div key={item.id}>{renderCard(item)}</div>
        ))}
      </Grid>
    );
  }

  return (
    <MotionStagger className={cn('w-full', className)}>
      <Grid cols={cols} gap={6}>
        {items.map((item) => (
          <MotionStaggerItem key={item.id} className="h-full">
            {renderCard(item)}
          </MotionStaggerItem>
        ))}
      </Grid>
    </MotionStagger>
  );
}
