import type { ReactNode } from 'react';

import {
  Card,
  CardDescription,
  CardHeader,
  CardTitle,
  IconWrapper,
  Surface,
} from '@/components/primitives';
import { cn } from '@/lib/utils';

/**
 * ValueCard — value proposition with icon emphasis.
 */
export type ValueCardProps = {
  heading: string;
  description: string;
  icon?: ReactNode;
  className?: string;
};

export function ValueCard({ heading, description, icon, className }: ValueCardProps) {
  return (
    <Card variant="elevated" className={cn('h-full', className)}>
      <CardHeader>
        {icon ? (
          <Surface
            tone="muted"
            padding="sm"
            className="border-domain-accent/20 bg-domain-accent/10 mb-3 inline-flex size-11 items-center justify-center rounded-lg border p-0"
          >
            <IconWrapper tone="domain" size="md">
              {icon}
            </IconWrapper>
          </Surface>
        ) : (
          /* Accent marker keeps identity when no icon is provided. */
          <span
            aria-hidden="true"
            className="bg-domain-accent/70 mb-1 block h-1 w-8 rounded-full"
          />
        )}
        <CardTitle>{heading}</CardTitle>
        <CardDescription>{description}</CardDescription>
      </CardHeader>
    </Card>
  );
}
