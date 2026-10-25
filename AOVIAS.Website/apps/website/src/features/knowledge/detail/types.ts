import type { ContentItem, TocItem } from '@/components/content';

export type KnowledgeBodySection = {
  id: string;
  heading?: string;
  paragraphs: readonly string[];
};

export type KnowledgeDetailContent = {
  item: ContentItem;
  /** URL slug segment after the section path. */
  slug: string;
  seo: {
    title: string;
    description: string;
    keywords?: readonly string[];
  };
  body: readonly KnowledgeBodySection[];
  toc: readonly TocItem[];
  relatedIds?: readonly string[];
};

export function buildTocFromBody(
  body: readonly KnowledgeBodySection[],
): TocItem[] {
  return body
    .filter((section) => Boolean(section.heading))
    .map((section) => ({
      id: section.id,
      title: section.heading as string,
      level: 2 as const,
      href: `#${section.id}`,
    }));
}
