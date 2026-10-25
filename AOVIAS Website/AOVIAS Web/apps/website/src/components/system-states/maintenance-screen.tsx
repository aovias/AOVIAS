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
 * MaintenanceScreen — planned maintenance or temporary downtime messaging.
 */
export type MaintenanceScreenProps = {
  title: string;
  description?: string;
  /** e.g. Planned maintenance | Temporary downtime */
  statusLabel?: string;
  estimatedAvailability?: string;
  contactLabel?: string;
  contactHref?: string;
  primaryAction?: StateAction;
  secondaryAction?: StateAction;
  illustration?: StateIllustration;
  className?: string;
  reveal?: boolean;
};

export function MaintenanceScreen({
  title,
  description,
  statusLabel = 'Maintenance',
  estimatedAvailability,
  contactLabel,
  contactHref,
  primaryAction,
  secondaryAction,
  illustration,
  className,
  reveal = true,
}: MaintenanceScreenProps) {
  const body = (
    <section
      className={cn('w-full py-section-y-xl', className)}
      aria-labelledby="maintenance-title"
    >
      <Container width="narrow">
        <Stack gap={6} align="center" className="text-center">
          <Text
            as="p"
            size="caption"
            weight="medium"
            tone="muted"
            className="tracking-wide uppercase"
          >
            {statusLabel}
          </Text>
          <IllustrationPlaceholder
            illustration={
              illustration ?? { label: 'Maintenance illustration' }
            }
          />
          <Stack gap={3} align="center">
            <Heading level={1} size="h1" id="maintenance-title">
              {title}
            </Heading>
            {description ? (
              <Paragraph tone="muted" className="text-center">
                {description}
              </Paragraph>
            ) : null}
          </Stack>
          {estimatedAvailability ? (
            <Text as="p" size="small">
              Estimated availability:{' '}
              <Text as="span" weight="medium">
                {estimatedAvailability}
              </Text>
            </Text>
          ) : null}
          {contactLabel && contactHref ? (
            <Text as="p" size="small" tone="muted">
              Contact:{' '}
              <a
                href={contactHref}
                className="text-primary font-medium underline-offset-4 hover:underline"
              >
                {contactLabel}
              </a>
            </Text>
          ) : null}
          <StateActions
            {...(primaryAction !== undefined ? { primary: primaryAction } : {})}
            {...(secondaryAction !== undefined
              ? { secondary: secondaryAction }
              : {})}
          />
        </Stack>
      </Container>
    </section>
  );

  void reveal;
  return body;
}
