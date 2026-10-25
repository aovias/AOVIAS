import { EmptyState } from '@/components/system-states/empty-state';
import type {
  NoResultsVariant,
  StateAction,
  StateIllustration,
} from '@/components/system-states/types';

/**
 * NoResults — generic / filtered / category empty lists with recovery CTAs.
 */
export type NoResultsProps = {
  title: string;
  description?: string;
  variant?: NoResultsVariant;
  /** Optional context label (filter name, category name). */
  contextLabel?: string;
  suggestions?: readonly string[];
  primaryAction?: StateAction;
  secondaryAction?: StateAction;
  illustration?: StateIllustration;
  className?: string;
  reveal?: boolean;
};

export function NoResults({
  title,
  description,
  variant = 'generic',
  contextLabel,
  suggestions,
  primaryAction,
  secondaryAction,
  illustration,
  className,
  reveal = true,
}: NoResultsProps) {
  const resolvedDescription = [
    description,
    contextLabel
      ? variant === 'filtered'
        ? `Active filters: ${contextLabel}.`
        : variant === 'category'
          ? `Category: ${contextLabel}.`
          : contextLabel
      : undefined,
    suggestions && suggestions.length > 0
      ? `Try: ${suggestions.join(' · ')}`
      : undefined,
  ]
    .filter(Boolean)
    .join(' ');

  const body = (
    <EmptyState
      title={title}
      {...(resolvedDescription ? { description: resolvedDescription } : {})}
      {...(illustration !== undefined ? { illustration } : {})}
      {...(primaryAction !== undefined ? { primaryAction } : {})}
      {...(secondaryAction !== undefined ? { secondaryAction } : {})}
      variant="default"
      reveal={false}
      {...(className !== undefined ? { className } : {})}
    />
  );

  void reveal;
  return body;
}
