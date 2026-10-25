'use client';

import { MotionReveal } from '@/lib/motion';
import { Alert, Button, Stack } from '@/components/primitives';
import { cn } from '@/lib/utils';

export type FormErrorProps = {
  message: string;
  retryable?: boolean;
  onRetry?: () => void;
  retryLabel?: string;
  className?: string;
};

/**
 * Form-level error state — submission failures with optional retry.
 */
export function FormErrorBanner({
  message,
  retryable = true,
  onRetry,
  retryLabel = 'Try again',
  className,
}: FormErrorProps) {
  return (
    <MotionReveal name="fadeIn">
      <Stack gap={3} className={cn(className)}>
        <Alert tone="error" title="Submission failed" live="assertive">
          {message}
        </Alert>
        {retryable && onRetry ? (
          <div>
            <Button type="button" variant="outline" onClick={onRetry}>
              {retryLabel}
            </Button>
          </div>
        ) : null}
      </Stack>
    </MotionReveal>
  );
}
