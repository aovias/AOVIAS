import { Search } from 'lucide-react';

import { StateActions } from '@/components/system-states/state-actions';
import type { StateAction } from '@/components/system-states/types';
import {
  Heading,
  IconWrapper,
  Paragraph,
  Stack,
  Text,
} from '@/components/primitives';
import { cn } from '@/lib/utils';

/**
 * SearchEmptyState — empty search with query display and recovery actions.
 */
export type SearchEmptyStateProps = {
  query?: string;
  title: string;
  description?: string;
  suggestions?: readonly string[];
  clearSearchAction?: StateAction;
  browseAction?: StateAction;
  className?: string;
  reveal?: boolean;
};

export function SearchEmptyState({
  query,
  title,
  description,
  suggestions,
  clearSearchAction,
  browseAction,
  className,
  reveal = true,
}: SearchEmptyStateProps) {
  const body = (
    <div
      role="status"
      className={cn(
        'border-border bg-muted/30 flex flex-col items-center rounded-lg border border-dashed px-6 py-12 text-center',
        className,
      )}
    >
      <Stack gap={4} align="center" className="max-w-lg">
        <IconWrapper tone="muted" size="xl" decorative>
          <Search />
        </IconWrapper>
        <Stack gap={2} align="center">
          <Heading level={2} size="h3">
            {title}
          </Heading>
          {query ? (
            <Text as="p" size="small" tone="muted">
              No matches for “{query}”.
            </Text>
          ) : null}
          {description ? (
            <Paragraph tone="muted" className="text-center">
              {description}
            </Paragraph>
          ) : null}
        </Stack>
        {suggestions && suggestions.length > 0 ? (
          <Stack gap={2} className="w-full text-start">
            <Text as="p" size="caption" weight="medium" tone="muted">
              Suggestions
            </Text>
            <ul className="text-small list-disc space-y-1 ps-5 text-start">
              {suggestions.map((item) => (
                <li key={item}>{item}</li>
              ))}
            </ul>
          </Stack>
        ) : null}
        <StateActions
          {...(clearSearchAction !== undefined
            ? { primary: clearSearchAction }
            : {})}
          {...(browseAction !== undefined ? { secondary: browseAction } : {})}
          primaryVariant="secondary"
        />
      </Stack>
    </div>
  );

  void reveal;
  return body;
}
