import type { ReactNode } from 'react';

import { Stack } from '@/components/primitives/layout/stack';
import { Heading } from '@/components/primitives/typography/heading';
import { Paragraph } from '@/components/primitives/typography/paragraph';
import { cn } from '@/lib/utils';

/**
 * EmptyState — generic empty/zero-data region. Pass title/description/action; no domain copy.
 */
export type EmptyStateProps = {
  title: string;
  description?: string;
  icon?: ReactNode;
  action?: ReactNode;
  className?: string;
};

export function EmptyState({
  title,
  description,
  icon,
  action,
  className,
}: EmptyStateProps) {
  return (
    <div
      className={cn(
        'border-border bg-muted/30 flex flex-col items-center justify-center rounded-lg border border-dashed px-6 py-12 text-center',
        className,
      )}
      role="status"
    >
      <Stack gap={3} align="center">
        {icon ? <div className="text-muted-foreground">{icon}</div> : null}
        <Heading level={3} size="h5">
          {title}
        </Heading>
        {description ? (
          <Paragraph size="small" tone="muted" className="max-w-md">
            {description}
          </Paragraph>
        ) : null}
        {action ? <div className="mt-2">{action}</div> : null}
      </Stack>
    </div>
  );
}
