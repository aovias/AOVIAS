import { IllustrationPlaceholder } from '@/components/system-states/illustration-placeholder';
import { StateActions } from '@/components/system-states/state-actions';
import type {
  EmptyStateVariant,
  StateAction,
  StateIllustration,
} from '@/components/system-states/types';
import { Heading, Paragraph, Stack } from '@/components/primitives';
import { cn } from '@/lib/utils';

/**
 * EmptyState — calm zero-data panel with optional illustration and CTAs.
 * Prefer this over inventing page-local empty UIs.
 */
export type SystemEmptyStateProps = {
  title: string;
  description?: string;
  illustration?: StateIllustration;
  primaryAction?: StateAction;
  secondaryAction?: StateAction;
  variant?: EmptyStateVariant;
  className?: string;
  reveal?: boolean;
};

const variantClass: Record<EmptyStateVariant, string> = {
  default: 'border-border bg-muted/30 border border-dashed',
  subtle: 'bg-muted/20',
  plain: '',
};

export function EmptyState({
  title,
  description,
  illustration,
  primaryAction,
  secondaryAction,
  variant = 'default',
  className,
  reveal = true,
}: SystemEmptyStateProps) {
  const body = (
    <div
      role="status"
      className={cn(
        'flex flex-col items-center justify-center rounded-lg px-6 py-12 text-center',
        variantClass[variant],
        className,
      )}
    >
      <Stack gap={4} align="center" className="max-w-lg">
        <IllustrationPlaceholder
          {...(illustration !== undefined ? { illustration } : {})}
        />
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
          {...(primaryAction !== undefined ? { primary: primaryAction } : {})}
          {...(secondaryAction !== undefined
            ? { secondary: secondaryAction }
            : {})}
        />
      </Stack>
    </div>
  );

  void reveal;
  return body;
}
