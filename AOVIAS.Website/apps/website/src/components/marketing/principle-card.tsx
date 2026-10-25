import type { ReactNode } from 'react';

import {
  Card,
  CardDescription,
  CardHeader,
  CardTitle,
  IconWrapper,
} from '@/components/primitives';
import { cn } from '@/lib/utils';

/**
 * PrincipleCard — principle name with supporting description.
 */
export type PrincipleCardProps = {
  principle: string;
  description: string;
  icon?: ReactNode;
  className?: string;
};

export function PrincipleCard({
  principle,
  description,
  icon,
  className,
}: PrincipleCardProps) {
  return (
    <Card variant="default" className={cn('h-full', className)}>
      <CardHeader>
        {icon ? (
          <IconWrapper tone="domain" size="lg" className="mb-2">
            {icon}
          </IconWrapper>
        ) : (
          /* Accent marker keeps identity when no icon is provided. */
          <span
            aria-hidden="true"
            className="bg-domain-accent/70 mb-1 block h-1 w-8 rounded-full"
          />
        )}
        <CardTitle>{principle}</CardTitle>
        <CardDescription>{description}</CardDescription>
      </CardHeader>
    </Card>
  );
}
