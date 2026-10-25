import type { ContentItem, ContentKind, SearchResult } from '@/components/content';

import { knowledgeCatalog } from '@/features/knowledge/domain/catalog';

export type KnowledgeSearchFilters = {
  query?: string;
  kinds?: readonly ContentKind[];
  categorySlug?: string;
};

function collectHighlights(text: string, terms: readonly string[]): string[] {
  const lower = text.toLowerCase();
  return terms.filter((term) => term && lower.includes(term.toLowerCase()));
}

/**
 * Frontend knowledge search — no backend index.
 * Filters the shared catalog by query, kind, and category.
 */
export function searchKnowledge(
  filters: KnowledgeSearchFilters = {},
): SearchResult[] {
  const query = filters.query?.trim() ?? '';
  const terms = query
    ? query.split(/\s+/).filter((term) => term.length > 1)
    : [];
  const kindSet =
    filters.kinds && filters.kinds.length > 0
      ? new Set(filters.kinds)
      : null;

  return knowledgeCatalog
    .filter((item) => {
      if (kindSet && !kindSet.has(item.kind)) return false;
      if (
        filters.categorySlug &&
        item.category?.slug !== filters.categorySlug
      ) {
        return false;
      }
      if (!query) return true;

      const haystack = [
        item.title,
        item.description ?? '',
        item.category?.name ?? '',
        ...(item.tags?.map((tag) => tag.name) ?? []),
      ]
        .join(' ')
        .toLowerCase();

      return terms.every((term) => haystack.includes(term.toLowerCase()));
    })
    .map((item) => toSearchResult(item, terms));
}

export function toSearchResult(
  item: ContentItem,
  terms: readonly string[] = [],
): SearchResult {
  const excerpt = item.description ?? item.title;
  const highlightSource = `${item.title} ${excerpt}`;
  const highlights = collectHighlights(highlightSource, terms);

  return {
    id: item.id,
    title: item.title,
    href: item.href,
    excerpt,
    kind: item.kind,
    ...(item.category ? { category: item.category } : {}),
    ...(highlights.length > 0 ? { highlights } : {}),
  };
}

export const knowledgeKindLabels: Record<ContentKind, string> = {
  article: 'Blog',
  insight: 'Insights',
  documentation: 'Documentation',
  resource: 'Resources',
  'case-study': 'Case studies',
  whitepaper: 'Whitepapers',
  'release-note': 'Release notes',
  'academy-lesson': 'Academy',
};
