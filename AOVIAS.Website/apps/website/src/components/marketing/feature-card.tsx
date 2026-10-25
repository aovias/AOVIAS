import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  IconWrapper,
  Link,
  type CardAccent,
} from '@/components/primitives';
import { cn } from '@/lib/utils';
import type { ReactNode } from 'react';

/**
 * FeatureCard — icon + title + description with optional link.
 * Hover lift via CSS `em-lift` (V11 — Framer off the shared card path).
 */
export type FeatureCardProps = {
  title: string;
  description: string;
  icon?: ReactNode;
  href?: string;
  linkLabel?: string;
  className?: string;
  /** Category identity for accent bar / icon tone. */
  accent?: CardAccent;
};

export function FeatureCard({
  title,
  description,
  icon,
  href,
  linkLabel = 'Learn more',
  className,
  accent,
}: FeatureCardProps) {
  return (
    <Card
      variant={href ? 'interactive' : 'elevated'}
      {...(accent ? { accent } : {})}
      className={cn('em-lift h-full', className)}
    >
      <CardHeader>
        {icon ? (
          <IconWrapper tone="domain" size="lg" className="mb-2">
            {icon}
          </IconWrapper>
        ) : null}
        <CardTitle>{title}</CardTitle>
        <CardDescription>{description}</CardDescription>
      </CardHeader>
      {href ? (
        <CardContent className="pt-0">
          <Link href={href} tone="primary" className="text-small font-medium">
            {linkLabel}
          </Link>
        </CardContent>
      ) : null}
    </Card>
  );
}
