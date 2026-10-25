import type { ReactNode } from 'react';

import { Grid, Text } from '@/components/primitives';
import { cn } from '@/lib/utils';

/**
 * LogoCloud — responsive partner/customer mark grid.
 * Pass logos as items (nodes or placeholder labels) — theme-aware via tokens.
 */
export type LogoCloudItem = {
  id: string;
  /** Accessible name for the organization. */
  name: string;
  /** Custom logo node (SVG/Image). When omitted, a placeholder is shown. */
  logo?: ReactNode;
};

export type LogoCloudProps = {
  items: readonly LogoCloudItem[];
  /** Visual density of logo cells. */
  size?: 'sm' | 'md' | 'lg';
  className?: string;
  label?: string;
};

const sizeClass = {
  sm: 'min-h-14 px-3 text-caption',
  md: 'min-h-20 px-4 text-small',
  lg: 'min-h-24 px-6 text-body',
} as const;

export function LogoCloud({
  items,
  size = 'md',
  className,
  label = 'Logos',
}: LogoCloudProps) {
  return (
    <div className={cn(className)} role="group" aria-label={label}>
      <Grid cols={items.length <= 3 ? 3 : 4} gap={4}>
        {items.map((item) => (
          <div
            key={item.id}
            className={cn(
              'border-border bg-muted/40 text-muted-foreground flex items-center justify-center rounded-md border',
              'dark:bg-muted/30',
              sizeClass[size],
            )}
          >
            {item.logo ? (
              <>
                <div
                  className="flex max-h-10 items-center justify-center opacity-80 grayscale transition-opacity hover:opacity-100 hover:grayscale-0"
                  aria-hidden="true"
                >
                  {item.logo}
                </div>
                <span className="sr-only">{item.name}</span>
              </>
            ) : (
              <Text as="span" size="small" tone="muted" weight="medium">
                {item.name}
              </Text>
            )}
          </div>
        ))}
      </Grid>
    </div>
  );
}
