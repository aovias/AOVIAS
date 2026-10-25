import Link from 'next/link';

import type { SearchResult } from '@/components/content';
import { MarketingReveal } from '@/components/marketing/reveal';
import { CategoryBadge } from '@/components/resources/category-badge';
import { splitHighlightedText } from '@/components/resources/utils';
import { Stack, Text } from '@/components/primitives';
import { cn } from '@/lib/utils';

/**
 * SearchResults — content-agnostic result list with highlight + empty state.
 */
export type SearchResultsProps = {
  results: readonly SearchResult[];
  query?: string;
  totalCount?: number;
  className?: string;
  emptyTitle?: string;
  emptyDescription?: string;
  reveal?: boolean;
};

export function SearchResults({
  results,
  query,
  totalCount,
  className,
  emptyTitle = 'No results found',
  emptyDescription = 'Try a different keyword or clear filters.',
  reveal = true,
}: SearchResultsProps) {
  const count = totalCount ?? results.length;

  const body =
    results.length === 0 ? (
      <div
        role="status"
        className="border-border bg-muted/30 rounded-lg border border-dashed px-6 py-10 text-center"
      >
        <Text as="p" weight="medium">
          {emptyTitle}
        </Text>
        <Text as="p" size="small" tone="muted" className="mt-2">
          {emptyDescription}
        </Text>
        {query ? (
          <Text as="p" size="caption" tone="muted" className="mt-3">
            No matches for “{query}”.
          </Text>
        ) : null}
      </div>
    ) : (
      <Stack gap={4}>
        <p className="text-muted-foreground text-small" role="status">
          {count} result{count === 1 ? '' : 's'}
          {query ? <> for “{query}”</> : null}
        </p>
        <ol className="flex flex-col gap-3">
          {results.map((result) => (
            <li
              key={result.id}
              className="border-border bg-card hover:bg-muted/30 rounded-lg border p-4 transition-colors"
            >
              <Stack gap={2}>
                <Stack direction="horizontal" gap={2} wrap align="center">
                  {result.category ? (
                    <CategoryBadge category={result.category} />
                  ) : null}
                  {result.kind ? (
                    <Text as="span" size="caption" tone="muted" className="capitalize">
                      {result.kind.replace('-', ' ')}
                    </Text>
                  ) : null}
                </Stack>
                <Link
                  href={result.href}
                  className="text-foreground focus-visible:ring-ring text-small font-semibold underline-offset-4 hover:underline focus-visible:ring-2 focus-visible:outline-none"
                >
                  {result.title}
                </Link>
                <p className="text-muted-foreground text-small leading-relaxed">
                  {splitHighlightedText(
                    result.excerpt,
                    result.highlights,
                  ).map((part, index) =>
                    part.match ? (
                      <mark
                        key={`${result.id}-${index}`}
                        className="bg-primary/15 text-foreground rounded-sm px-0.5"
                      >
                        {part.text}
                      </mark>
                    ) : (
                      <span key={`${result.id}-${index}`}>{part.text}</span>
                    ),
                  )}
                </p>
              </Stack>
            </li>
          ))}
        </ol>
      </Stack>
    );

  return (
    <section
      aria-label="Search results"
      className={cn(className)}
    >
      {reveal ? <MarketingReveal enabled>{body}</MarketingReveal> : body}
    </section>
  );
}
