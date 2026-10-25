import { cn } from '@/lib/utils';

/**
 * Separator — semantic horizontal/vertical rule for lists and card internals.
 */
export type SeparatorProps = {
  className?: string;
  orientation?: 'horizontal' | 'vertical';
  decorative?: boolean;
};

export function Separator({
  className,
  orientation = 'horizontal',
  decorative = true,
}: SeparatorProps) {
  return (
    <div
      role={decorative ? 'none' : 'separator'}
      aria-orientation={decorative ? undefined : orientation}
      className={cn(
        'bg-border shrink-0',
        orientation === 'horizontal' ? 'h-px w-full' : 'h-full w-px self-stretch',
        className,
      )}
    />
  );
}
