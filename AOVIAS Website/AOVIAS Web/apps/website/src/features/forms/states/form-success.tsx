'use client';

import { MotionReveal } from '@/lib/motion';
import { Button, Heading, Paragraph, Stack, Text } from '@/components/primitives';
import { cn } from '@/lib/utils';

export type FormSuccessProps = {
  title?: string;
  message?: string;
  followUp?: string;
  showIllustration?: boolean;
  onReset?: () => void;
  resetLabel?: string;
  className?: string;
};

/**
 * Success state — calm confirmation for institutional form submissions.
 */
export function FormSuccess({
  title = 'Request received',
  message = 'Thank you. We received your submission and will respond with care.',
  followUp,
  showIllustration = false,
  onReset,
  resetLabel = 'Submit another response',
  className,
}: FormSuccessProps) {
  return (
    <MotionReveal name="fadeIn">
      <div
        role="status"
        aria-live="polite"
        className={cn(
          'border-success/30 bg-success-subtle text-success-foreground rounded-lg border p-6',
          className,
        )}
      >
        <Stack gap={4}>
          {showIllustration ? (
            <div
              aria-hidden="true"
              className="border-success/40 bg-background/40 flex h-16 items-center justify-center rounded-md border"
            >
              <Text as="span" size="caption" tone="muted">
                Received
              </Text>
            </div>
          ) : null}
          <Stack gap={2}>
            <Heading level={2} size="h3">
              {title}
            </Heading>
            <Paragraph>{message}</Paragraph>
            {followUp ? <Paragraph tone="muted">{followUp}</Paragraph> : null}
          </Stack>
          {onReset ? (
            <div>
              <Button type="button" variant="outline" onClick={onReset}>
                {resetLabel}
              </Button>
            </div>
          ) : null}
        </Stack>
      </div>
    </MotionReveal>
  );
}
