import type { ReactNode } from 'react';

import type { TrustBadgeSize, TrustBadgeVariant, TrustSignal } from '@/components/trust/models';
import { IconWrapper, Stack, Text } from '@/components/primitives';
import { cn } from '@/lib/utils';

/**
 * TrustBadge — compact credibility signal from a TrustSignal model.
 */
export type TrustBadgeProps = {
  signal: TrustSignal;
  size?: TrustBadgeSize;
  className?: string;
  /** Override icon from signal. */
  icon?: ReactNode;
};

const variantClass: Record<TrustBadgeVariant, string> = {
  default: 'border-border bg-muted/50 text-foreground',
  /* Security → emerald identity */
  security: 'border-success/30 bg-success/10 text-foreground',
  /* Compliance → enterprise blue identity */
  compliance: 'border-primary/25 bg-primary-muted/50 text-foreground',
  /* Quality → supporting accent identity */
  quality: 'border-secondary/30 bg-secondary-muted/60 text-foreground',
};

const sizeClass: Record<TrustBadgeSize, string> = {
  sm: 'gap-2 px-2.5 py-1.5',
  md: 'gap-2.5 px-3 py-2',
  lg: 'gap-3 px-4 py-3',
};

export function TrustBadge({
  signal,
  size = 'md',
  className,
  icon,
}: TrustBadgeProps) {
  const variant = signal.variant ?? 'default';
  const resolvedIcon = icon ?? signal.icon;

  return (
    <div
      className={cn(
        'inline-flex max-w-full items-start rounded-md border',
        variantClass[variant],
        sizeClass[size],
        className,
      )}
    >
      {resolvedIcon ? (
        <IconWrapper
          tone={
            variant === 'security'
              ? 'success'
              : variant === 'compliance'
                ? 'primary'
                : variant === 'quality'
                  ? 'domain'
                  : 'muted'
          }
          size={size === 'lg' ? 'md' : 'sm'}
          className="mt-0.5 shrink-0"
        >
          {resolvedIcon}
        </IconWrapper>
      ) : null}
      <Stack gap={1} className="min-w-0">
        <Text
          as="span"
          weight="medium"
          size={size === 'sm' ? 'caption' : 'small'}
        >
          {signal.label}
        </Text>
        {signal.description ? (
          <Text as="span" size="caption" tone="muted">
            {signal.description}
          </Text>
        ) : null}
      </Stack>
    </div>
  );
}
