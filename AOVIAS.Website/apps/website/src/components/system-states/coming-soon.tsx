import type { ReactNode } from 'react';

import { IllustrationPlaceholder } from '@/components/system-states/illustration-placeholder';
import { StateActions } from '@/components/system-states/state-actions';
import type {
  StateAction,
  StateIllustration,
} from '@/components/system-states/types';
import {
  Container,
  Heading,
  Paragraph,
  Stack,
  Text,
} from '@/components/primitives';
import { cn } from '@/lib/utils';

/**
 * ComingSoon — pre-launch surface with CTA and optional placeholder slots.
 */
export type ComingSoonProps = {
  title: string;
  description?: string;
  primaryAction?: StateAction;
  secondaryAction?: StateAction;
  illustration?: StateIllustration;
  /** Reserved slot for a future countdown (pass node; no countdown logic). */
  countdownPlaceholder?: ReactNode;
  /** Reserved slot for newsletter form composition. */
  newsletterPlaceholder?: ReactNode;
  className?: string;
  reveal?: boolean;
};

export function ComingSoon({
  title,
  description,
  primaryAction,
  secondaryAction,
  illustration,
  countdownPlaceholder,
  newsletterPlaceholder,
  className,
  reveal = true,
}: ComingSoonProps) {
  const body = (
    <section
      className={cn('w-full py-section-y-xl', className)}
      aria-labelledby="coming-soon-title"
    >
      <Container width="narrow">
        <Stack gap={6} align="center" className="text-center">
          <IllustrationPlaceholder
            illustration={illustration ?? { label: 'Coming soon illustration' }}
          />
          <Stack gap={3} align="center">
            <Heading level={1} size="h1" id="coming-soon-title">
              {title}
            </Heading>
            {description ? (
              <Paragraph tone="muted" className="text-center">
                {description}
              </Paragraph>
            ) : null}
          </Stack>
          {countdownPlaceholder ? (
            <div className="border-border bg-muted/30 w-full max-w-md rounded-lg border border-dashed px-4 py-6">
              {countdownPlaceholder}
            </div>
          ) : null}
          <StateActions
            {...(primaryAction !== undefined ? { primary: primaryAction } : {})}
            {...(secondaryAction !== undefined
              ? { secondary: secondaryAction }
              : {})}
          />
          {newsletterPlaceholder ? (
            <div className="w-full max-w-md pt-2">{newsletterPlaceholder}</div>
          ) : null}
        </Stack>
      </Container>
    </section>
  );

  void reveal;
  return body;
}

/** Default countdown placeholder chrome for design-system QA. */
export function CountdownPlaceholder({ label = 'Countdown placeholder' }: { label?: string }) {
  return (
    <Text as="p" size="caption" tone="muted">
      {label}
    </Text>
  );
}
