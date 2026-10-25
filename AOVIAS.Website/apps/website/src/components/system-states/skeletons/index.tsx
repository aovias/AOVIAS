import { Skeleton, Stack, Grid, type GridCols } from '@/components/primitives';
import { cn } from '@/lib/utils';

/**
 * Skeleton System — composed placeholders over the Skeleton primitive.
 */

export type TextSkeletonProps = {
  lines?: number;
  className?: string;
};

export function TextSkeleton({ lines = 3, className }: TextSkeletonProps) {
  return (
    <Stack gap={2} className={cn(className)} aria-hidden="true">
      {Array.from({ length: lines }).map((_, index) => (
        <Skeleton
          key={index}
          label="Loading text"
          className={cn(
            'h-3',
            index === lines - 1 ? 'w-2/3' : 'w-full',
            index === 0 && 'h-4 w-1/2',
          )}
        />
      ))}
    </Stack>
  );
}

export type CardSkeletonProps = {
  className?: string;
  withMedia?: boolean;
};

export function CardSkeleton({
  className,
  withMedia = true,
}: CardSkeletonProps) {
  return (
    <div
      className={cn(
        'border-border bg-card overflow-hidden rounded-lg border p-0',
        className,
      )}
    >
      {withMedia ? <Skeleton className="aspect-video w-full rounded-none" /> : null}
      <div className="space-y-3 p-5">
        <Skeleton className="h-3 w-1/3" />
        <Skeleton className="h-5 w-3/4" />
        <TextSkeleton lines={2} />
      </div>
    </div>
  );
}

export type ImageSkeletonProps = {
  className?: string;
  aspect?: 'video' | 'square' | 'wide';
};

const aspectClass = {
  video: 'aspect-video',
  square: 'aspect-square',
  wide: 'aspect-[21/9]',
} as const;

export function ImageSkeleton({
  className,
  aspect = 'video',
}: ImageSkeletonProps) {
  return (
    <Skeleton
      className={cn('w-full', aspectClass[aspect], className)}
      label="Loading image"
    />
  );
}

export type GridSkeletonProps = {
  count?: number;
  cols?: Extract<GridCols, 2 | 3 | 4>;
  className?: string;
};

export function GridSkeleton({
  count = 6,
  cols = 3,
  className,
}: GridSkeletonProps) {
  return (
    <Grid cols={cols} gap={6} className={cn(className)}>
      {Array.from({ length: count }).map((_, index) => (
        <CardSkeleton key={index} />
      ))}
    </Grid>
  );
}

export type ListSkeletonProps = {
  rows?: number;
  className?: string;
};

export function ListSkeleton({ rows = 5, className }: ListSkeletonProps) {
  return (
    <Stack gap={3} className={cn(className)}>
      {Array.from({ length: rows }).map((_, index) => (
        <div
          key={index}
          className="border-border flex items-center gap-3 rounded-md border p-3"
        >
          <Skeleton className="size-10 shrink-0 rounded-full" />
          <div className="min-w-0 flex-1 space-y-2">
            <Skeleton className="h-3 w-1/3" />
            <Skeleton className="h-3 w-2/3" />
          </div>
        </div>
      ))}
    </Stack>
  );
}

export type HeroSkeletonProps = {
  className?: string;
};

export function HeroSkeleton({ className }: HeroSkeletonProps) {
  return (
    <div className={cn('w-full space-y-6 py-10', className)}>
      <Skeleton className="h-4 w-24" />
      <Skeleton className="h-10 w-3/4 max-w-xl" />
      <Skeleton className="h-4 w-full max-w-lg" />
      <Skeleton className="h-4 w-2/3 max-w-md" />
      <div className="flex gap-3 pt-2">
        <Skeleton className="h-9 w-28" />
        <Skeleton className="h-9 w-28" />
      </div>
      <ImageSkeleton aspect="wide" className="mt-6" />
    </div>
  );
}

export type TableSkeletonProps = {
  rows?: number;
  columns?: number;
  className?: string;
};

export function TableSkeleton({
  rows = 5,
  columns = 4,
  className,
}: TableSkeletonProps) {
  return (
    <div
      className={cn('border-border w-full overflow-hidden rounded-lg border', className)}
      role="status"
      aria-label="Loading table"
      aria-busy="true"
    >
      <div className="bg-muted/40 border-border grid gap-3 border-b px-4 py-3"
        style={{ gridTemplateColumns: `repeat(${columns}, minmax(0, 1fr))` }}
      >
        {Array.from({ length: columns }).map((_, index) => (
          <Skeleton key={`h-${index}`} className="h-3 w-2/3" />
        ))}
      </div>
      {Array.from({ length: rows }).map((_, row) => (
        <div
          key={row}
          className="border-border grid gap-3 border-b px-4 py-3 last:border-b-0"
          style={{ gridTemplateColumns: `repeat(${columns}, minmax(0, 1fr))` }}
        >
          {Array.from({ length: columns }).map((_, col) => (
            <Skeleton key={`${row}-${col}`} className="h-3 w-full" />
          ))}
        </div>
      ))}
    </div>
  );
}
