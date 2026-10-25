import type { IndustryProductShot } from '@/features/industries/editorial/types';
import { cn } from '@/lib/utils';

export type ProductExperienceGridProps = {
  items: readonly IndustryProductShot[];
  className?: string;
};

export function ProductExperienceGrid({
  items,
  className,
}: ProductExperienceGridProps) {
  return (
    <ul
      className={cn(
        'grid gap-5 sm:grid-cols-2 lg:grid-cols-3 lg:gap-6',
        className,
      )}
    >
      {items.map((item) => (
        <li
          key={item.id}
          className="border-border bg-card/70 group flex h-full flex-col overflow-hidden rounded-xl border shadow-surface"
        >
          <div className="border-border bg-surface-muted/80 relative aspect-[16/10] overflow-hidden border-b">
            {/* eslint-disable-next-line @next/next/no-img-element -- lazy product placeholders */}
            <img
              src={item.imageSrc}
              alt={item.imageAlt}
              className="size-full object-cover object-top transition-transform duration-500 group-hover:scale-[1.02]"
              loading="lazy"
              decoding="async"
            />
          </div>
          <div className="flex flex-1 flex-col gap-1.5 p-4">
            <h3 className="text-foreground text-body font-semibold tracking-tight">
              {item.title}
            </h3>
            <p className="text-muted-foreground text-small leading-relaxed">
              {item.description}
            </p>
          </div>
        </li>
      ))}
    </ul>
  );
}
