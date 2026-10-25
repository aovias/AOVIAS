'use client';

import { useEffect, useMemo, useState, useTransition } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';

import { KnowledgeCover } from '@/components/brand';
import type { ContentKind } from '@/components/content';
import { SectionHeader } from '@/components/marketing';
import { Container, Stack, Text } from '@/components/primitives';
import { CategoryBadge, Pagination, SearchResults } from '@/components/resources';
import { SearchEmptyState } from '@/components/system-states';
import {
  knowledgeCategories,
  knowledgeKindLabels,
  searchKnowledge,
} from '@/features/knowledge/domain';

const PAGE_SIZE = 6;

const kindFilters: ContentKind[] = [
  'article',
  'insight',
  'documentation',
  'resource',
  'release-note',
];

const kindValues = new Set<ContentKind>(kindFilters);

export type KnowledgeSearchPageProps = {
  query?: string;
  kind?: ContentKind | null;
  category?: string | null;
  page?: number;
};

/**
 * Presentational search experience — used for Suspense fallback and live URL state.
 */
export function KnowledgeSearchView({
  query = '',
  kind = null,
  category = null,
  page = 1,
}: KnowledgeSearchPageProps) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [draft, setDraft] = useState(query);

  useEffect(() => {
    setDraft(query);
  }, [query]);

  const results = useMemo(
    () =>
      searchKnowledge({
        query,
        ...(kind ? { kinds: [kind] } : {}),
        ...(category ? { categorySlug: category } : {}),
      }),
    [query, kind, category],
  );

  const totalPages = Math.max(1, Math.ceil(results.length / PAGE_SIZE));
  const currentPage = Math.min(page, totalPages);
  const pageItems = results.slice(
    (currentPage - 1) * PAGE_SIZE,
    currentPage * PAGE_SIZE,
  );

  function updateParams(next: {
    q?: string | null;
    kind?: string | null;
    category?: string | null;
    page?: string | null;
  }) {
    const params = new URLSearchParams();
    const nextQuery = next.q !== undefined ? next.q : query;
    const nextKind = next.kind !== undefined ? next.kind : kind;
    const nextCategory = next.category !== undefined ? next.category : category;
    const nextPage = next.page !== undefined ? next.page : String(page);

    if (nextQuery) params.set('q', nextQuery);
    if (nextKind) params.set('kind', nextKind);
    if (nextCategory) params.set('category', nextCategory);
    if (nextPage && nextPage !== '1') params.set('page', nextPage);

    const qs = params.toString();
    startTransition(() => {
      router.push(qs ? `/resources/search?${qs}` : '/resources/search');
    });
  }

  return (
    <section
      className="bg-muted/40 w-full py-12 lg:py-16 xl:py-20"
      aria-label="Knowledge search"
    >
      <Container width="wide">
        <Stack gap={8}>
          <div className="grid items-start gap-10 lg:grid-cols-[minmax(0,1.15fr)_minmax(0,0.85fr)] xl:gap-14">
            <SectionHeader
              eyebrow="Search"
              heading="Search AOVIAS Knowledge"
              description="Find articles, insights, documentation, and resources by topic, product, or practice."
            />
            <KnowledgeCover
              label="Find with confidence"
              tone="info"
              className="aspect-[4/3] max-md:hidden"
            />
          </div>

          <form
            role="search"
            className="flex flex-col gap-3 sm:flex-row"
            onSubmit={(event) => {
              event.preventDefault();
              updateParams({ q: draft.trim() || null, page: '1' });
            }}
          >
            <label className="sr-only" htmlFor="knowledge-search">
              Search query
            </label>
            <input
              id="knowledge-search"
              type="search"
              value={draft}
              onChange={(event) => {
                setDraft(event.target.value);
              }}
              placeholder="Search by topic, product, or practice…"
              className="border-border bg-background focus-visible:ring-ring w-full rounded-md border px-3 py-2 text-small focus-visible:ring-2 focus-visible:outline-none"
            />
            <button
              type="submit"
              className="bg-primary text-primary-foreground focus-visible:ring-ring rounded-md px-4 py-2 text-small font-medium focus-visible:ring-2 focus-visible:outline-none"
            >
              Search
            </button>
          </form>

          <div className="flex flex-wrap gap-2" aria-label="Filter by type">
            <button
              type="button"
              className="focus-visible:ring-ring rounded-md focus-visible:ring-2 focus-visible:outline-none"
              onClick={() => {
                updateParams({ kind: null, page: '1' });
              }}
            >
              <CategoryBadge
                category={{ id: 'all', name: 'All types', tone: 'default' }}
                active={!kind}
              />
            </button>
            {kindFilters.map((filterKind) => (
              <button
                key={filterKind}
                type="button"
                className="focus-visible:ring-ring rounded-md focus-visible:ring-2 focus-visible:outline-none"
                onClick={() => {
                  updateParams({ kind: filterKind, page: '1' });
                }}
              >
                <CategoryBadge
                  category={{
                    id: filterKind,
                    name: knowledgeKindLabels[filterKind],
                    tone: 'info',
                  }}
                  active={kind === filterKind}
                />
              </button>
            ))}
          </div>

          <div className="flex flex-wrap gap-2" aria-label="Filter by category">
            {knowledgeCategories.map((entry) => (
              <button
                key={entry.id}
                type="button"
                className="focus-visible:ring-ring rounded-md focus-visible:ring-2 focus-visible:outline-none"
                onClick={() => {
                  updateParams({
                    category: category === entry.slug ? null : (entry.slug ?? null),
                    page: '1',
                  });
                }}
              >
                <CategoryBadge
                  category={entry}
                  active={category === entry.slug}
                />
              </button>
            ))}
          </div>

          <div role="status" aria-live="polite">
            {isPending ? (
              <Text as="p" size="small" tone="muted">
                Updating results…
              </Text>
            ) : null}
          </div>

          {results.length === 0 ? (
            <SearchEmptyState
              {...(query ? { query } : {})}
              title="No knowledge matches"
              description="Try another keyword, clear filters, or browse a knowledge section."
              suggestions={['trust', 'Command', 'governance', 'documentation']}
              clearSearchAction={{
                label: 'Clear search',
                href: '/resources/search',
              }}
              browseAction={{
                label: 'Browse hub',
                href: '/resources',
              }}
              reveal={false}
            />
          ) : (
            <>
              <SearchResults
                results={pageItems}
                {...(query ? { query } : {})}
                totalCount={results.length}
                reveal={false}
              />
              {totalPages > 1 ? (
                <Pagination
                  currentPage={currentPage}
                  totalPages={totalPages}
                  getPageHref={(target) => {
                    const params = new URLSearchParams();
                    if (query) params.set('q', query);
                    if (kind) params.set('kind', kind);
                    if (category) params.set('category', category);
                    if (target > 1) params.set('page', String(target));
                    const qs = params.toString();
                    return qs
                      ? `/resources/search?${qs}`
                      : '/resources/search';
                  }}
                />
              ) : null}
            </>
          )}
        </Stack>
      </Container>
    </section>
  );
}

/**
 * Live search page — reads URL via useSearchParams (must be under Suspense).
 */
export function KnowledgeSearchPage() {
  const searchParams = useSearchParams();
  const query = searchParams.get('q') ?? '';
  const kindParam = searchParams.get('kind');
  const kind =
    kindParam && kindValues.has(kindParam as ContentKind)
      ? (kindParam as ContentKind)
      : null;
  const category = searchParams.get('category');
  const page = Math.max(1, Number(searchParams.get('page') ?? '1') || 1);

  return (
    <KnowledgeSearchView
      query={query}
      kind={kind}
      category={category}
      page={page}
    />
  );
}
