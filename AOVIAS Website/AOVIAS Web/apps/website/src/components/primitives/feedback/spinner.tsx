import { cn } from '@/lib/utils';

/**
 * Spinner — indeterminate loading indicator.
 */
export type SpinnerSize = 'sm' | 'md' | 'lg';

export type SpinnerProps = {
  size?: SpinnerSize;
  className?: string;
  label?: string;
};

const sizeClass: Record<SpinnerSize, string> = {
  sm: 'size-3.5 border-[1.5px]',
  md: 'size-4 border-2',
  lg: 'size-6 border-2',
};

export function Spinner({ size = 'md', className, label = 'Loading' }: SpinnerProps) {
  return (
    <span
      role="status"
      aria-label={label}
      className={cn(
        'border-muted-foreground/30 border-t-foreground inline-block animate-spin rounded-full',
        sizeClass[size],
        className,
      )}
    />
  );
}
