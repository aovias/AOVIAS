import { AlertCircle } from 'lucide-react';
import type { ReactNode } from 'react';

import { StateActions } from '@/components/system-states/state-actions';
import type {
  ErrorStateVariant,
  StateAction,
} from '@/components/system-states/types';
import {
  Heading,
  IconWrapper,
  Paragraph,
  Stack,
} from '@/components/primitives';
import { cn } from '@/lib/utils';

/**
 * ErrorState — recoverable failure presentation (no blame, clear next steps).
 */
export type ErrorStateProps = {
  title: string;
  description?: string;
  variant?: ErrorStateVariant;
  retryAction?: StateAction;
  supportAction?: StateAction;
  className?: string;
  reveal?: boolean;
  /** Override default icon. */
  icon?: ReactNode;
};

export function ErrorState({
  title,
  description,
  variant = 'generic',
  retryAction,
  supportAction,
  className,
  reveal = true,
  icon,
}: ErrorStateProps) {
  const body = (
    <div
      role="alert"
      className={cn(
        'border-error/30 bg-error-subtle/40 flex flex-col items-center justify-center rounded-lg border px-6 py-12 text-center',
        className,
      )}
      data-variant={variant}
    >
      <Stack gap={4} align="center" className="max-w-lg">
        <IconWrapper tone="error" size="xl" decorative>
          {icon ?? <AlertCircle />}
        </IconWrapper>
        <Stack gap={2} align="center">
          <Heading level={2} size="h3">
            {title}
          </Heading>
          {description ? (
            <Paragraph tone="muted" className="text-center">
              {description}
            </Paragraph>
          ) : null}
        </Stack>
        <StateActions
          {...(retryAction !== undefined ? { primary: retryAction } : {})}
          {...(supportAction !== undefined ? { secondary: supportAction } : {})}
          primaryVariant="primary"
        />
      </Stack>
    </div>
  );

  void reveal;
  return body;
}
