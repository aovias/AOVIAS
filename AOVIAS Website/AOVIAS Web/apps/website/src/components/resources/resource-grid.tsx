import type { ContentItem } from '@/components/content';
import { MarketingReveal } from '@/components/marketing/reveal';
import { ArticleCard } from '@/components/resources/article-card';
import { Grid, type GridCols } from '@/components/primitives';
import { cn } from '@/lib/utils';

/**
 * ResourceGrid — responsive ContentItem grid.
 * Filtering/sorting are parent responsibilities (filter-ready / sort-ready).
 */
export type ResourceGridProps = {
  items: readonly ContentItem[];
  cols?: Extract<GridCols, 2 | 3 | 4>;
  className?: string;
  reveal?: boolean;
  /** Accessible label for the list region. */
  label?: string;
  emptyMessage?: string;
};

export function ResourceGrid({
  items,
  cols = 3,
  className,
  reveal = true,
  label = 'Resources',
  emptyMessage = 'No resources match the current filters.',
}: ResourceGridProps) {
  if (items.length === 0) {
    return (
      <p
        role="status"
        className={cn('text-muted-foreground text-small', className)}
      >
        {emptyMessage}
      </p>
    );
  }

  const grid = (
    <div role="list">
      <Grid cols={cols} gap={6} className={cn('items-stretch', className)}>
        {items.map((item) => (
          <div key={item.id} role="listitem">
            <ArticleCard item={item} />
          </div>
        ))}
      </Grid>
    </div>
  );

  return (
    <section aria-label={label}>
      {reveal ? <MarketingReveal enabled>{grid}</MarketingReveal> : grid}
    </section>
  );
}
