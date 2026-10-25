import { cn } from '@/lib/utils';

/**
 * Skeleton — placeholder shimmer for loading content regions.
 */
export type SkeletonProps = {
  className?: string;
  /** Accessible label announced while loading. */
  label?: string;
};

export function Skeleton({ className, label = 'Loading' }: SkeletonProps) {
  return (
    <div
      role="status"
      aria-label={label}
      aria-busy="true"
      className={cn('bg-muted animate-pulse rounded-md', className)}
    />
  );
}
