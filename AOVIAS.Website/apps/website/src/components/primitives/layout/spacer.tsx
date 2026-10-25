import { cn } from '@/lib/utils';

/**
 * Spacer — token-driven empty space for flex/grid compositions.
 * Prefer Stack gap when siblings share uniform spacing.
 */
export type SpacerSize = 1 | 2 | 3 | 4 | 5 | 6 | 8 | 10 | 12;

export type SpacerProps = {
  size?: SpacerSize;
  axis?: 'y' | 'x' | 'both';
  className?: string;
};

const sizeY: Record<SpacerSize, string> = {
  1: 'h-1',
  2: 'h-2',
  3: 'h-3',
  4: 'h-4',
  5: 'h-5',
  6: 'h-6',
  8: 'h-8',
  10: 'h-10',
  12: 'h-12',
};

const sizeX: Record<SpacerSize, string> = {
  1: 'w-1',
  2: 'w-2',
  3: 'w-3',
  4: 'w-4',
  5: 'w-5',
  6: 'w-6',
  8: 'w-8',
  10: 'w-10',
  12: 'w-12',
};

export function Spacer({ size = 4, axis = 'y', className }: SpacerProps) {
  return (
    <div
      aria-hidden="true"
      className={cn(
        'shrink-0',
        (axis === 'y' || axis === 'both') && sizeY[size],
        (axis === 'x' || axis === 'both') && sizeX[size],
        className,
      )}
    />
  );
}
