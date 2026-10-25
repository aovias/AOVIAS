import type {
  ContentItem,
  ResourceSortDirection,
  ResourceSortKey,
} from '@/components/content';

export function formatPublishDate(
  iso?: string,
  locale = 'en-US',
): string | undefined {
  if (!iso) return undefined;
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return undefined;
  return new Intl.DateTimeFormat(locale, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  }).format(date);
}

export function formatReadingTime(minutes?: number): string | undefined {
  if (minutes === undefined || minutes <= 0) return undefined;
  return minutes === 1 ? '1 min read' : `${minutes} min read`;
}

/**
 * Client/parent-side sorting helper — keeps sort logic out of the grid UI.
 */
export function sortContentItems(
  items: readonly ContentItem[],
  key: ResourceSortKey,
  direction: ResourceSortDirection = 'desc',
): ContentItem[] {
  const factor = direction === 'asc' ? 1 : -1;
  return [...items].sort((a, b) => {
    if (key === 'title') {
      return a.title.localeCompare(b.title) * factor;
    }
    if (key === 'readingTime') {
      return ((a.readingTimeMinutes ?? 0) - (b.readingTimeMinutes ?? 0)) * factor;
    }
    const aTime = a.publishedAt ? Date.parse(a.publishedAt) : 0;
    const bTime = b.publishedAt ? Date.parse(b.publishedAt) : 0;
    return (aTime - bTime) * factor;
  });
}

export function filterContentByKind(
  items: readonly ContentItem[],
  kinds: readonly ContentItem['kind'][],
): ContentItem[] {
  if (kinds.length === 0) return [...items];
  const set = new Set(kinds);
  return items.filter((item) => set.has(item.kind));
}

/**
 * Split excerpt around highlight terms for SearchResults rendering.
 */
export function splitHighlightedText(
  text: string,
  highlights: readonly string[] = [],
): { text: string; match: boolean }[] {
  if (highlights.length === 0) return [{ text, match: false }];
  const escaped = highlights
    .filter(Boolean)
    .map((term) => term.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'));
  if (escaped.length === 0) return [{ text, match: false }];
  const pattern = new RegExp(`(${escaped.join('|')})`, 'gi');
  return text.split(pattern).filter(Boolean).map((part) => ({
    text: part,
    match: escaped.some((term) => new RegExp(`^${term}$`, 'i').test(part)),
  }));
}
