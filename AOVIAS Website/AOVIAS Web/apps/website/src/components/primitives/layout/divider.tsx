import { cn } from '@/lib/utils';

/**
 * Divider — thematic break between content blocks.
 * Prefer for layout rhythm; use Separator for card/list internals when needed.
 */
export type DividerProps = {
  className?: string;
  orientation?: 'horizontal' | 'vertical';
  decorative?: boolean;
};

export function Divider({
  className,
  orientation = 'horizontal',
  decorative = true,
}: DividerProps) {
  return (
    <div
      role={decorative ? 'none' : 'separator'}
      aria-orientation={decorative ? undefined : orientation}
      className={cn(
        'bg-border shrink-0',
        orientation === 'horizontal' ? 'h-px w-full' : 'h-full w-px min-h-4',
        className,
      )}
    />
  );
}
