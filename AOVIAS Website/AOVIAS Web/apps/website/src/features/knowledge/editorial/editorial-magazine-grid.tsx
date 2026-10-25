import type { ContentItem } from '@/components/content';
import { ArticleCard } from '@/components/resources/article-card';
import { cn } from '@/lib/utils';

export type EditorialMagazineGridProps = {
  items: readonly ContentItem[];
  className?: string;
  emptyMessage?: string;
};

/**
 * Magazine-style knowledge grid — featured lead + supporting cards.
 * Avoids monotonous equal-weight grids.
 */
export function EditorialMagazineGrid({
  items,
  className,
  emptyMessage = 'No content is available in this section yet.',
}: EditorialMagazineGridProps) {
  if (items.length === 0) {
    return (
      <p role="status" className="text-muted-foreground text-small">
        {emptyMessage}
      </p>
    );
  }

  const [lead, ...rest] = items;
  if (!lead) {
    return null;
  }

  return (
    <div
      className={cn('grid gap-6 lg:grid-cols-12 lg:gap-7', className)}
      role="list"
    >
      <div role="listitem" className="lg:col-span-7 xl:col-span-8">
        <ArticleCard item={lead} featured className="min-h-full" />
      </div>
      <div className="grid gap-6 sm:grid-cols-2 lg:col-span-5 lg:grid-cols-1 xl:col-span-4">
        {rest.slice(0, 2).map((item) => (
          <div key={item.id} role="listitem">
            <ArticleCard item={item} />
          </div>
        ))}
      </div>
      {rest.length > 2 ? (
        <div className="grid gap-6 sm:grid-cols-2 lg:col-span-12 lg:grid-cols-3">
          {rest.slice(2).map((item) => (
            <div key={item.id} role="listitem">
              <ArticleCard item={item} />
            </div>
          ))}
        </div>
      ) : null}
    </div>
  );
}
