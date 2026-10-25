import type { LoadingStateSize } from '@/components/system-states/types';
import { Spinner, Stack, Text } from '@/components/primitives';
import { cn } from '@/lib/utils';

/**
 * LoadingState — spinner and/or determinate progress with optional message.
 */
export type LoadingStateProps = {
  message?: string;
  size?: LoadingStateSize;
  /** 0–100 for determinate progress; omit for indeterminate spinner only. */
  progress?: number;
  className?: string;
  label?: string;
};

const spinnerSize: Record<LoadingStateSize, 'sm' | 'md' | 'lg'> = {
  sm: 'sm',
  md: 'md',
  lg: 'lg',
};

export function LoadingState({
  message,
  size = 'md',
  progress,
  className,
  label = 'Loading',
}: LoadingStateProps) {
  const hasProgress = typeof progress === 'number';
  const clamped = hasProgress
    ? Math.min(100, Math.max(0, Math.round(progress)))
    : undefined;

  return (
    <div
      role="status"
      aria-live="polite"
      aria-busy="true"
      aria-label={label}
      className={cn(
        'flex flex-col items-center justify-center gap-3 px-6 py-10',
        className,
      )}
    >
      <Stack gap={3} align="center" className="w-full max-w-sm">
        {!hasProgress ? (
          <Spinner size={spinnerSize[size]} label={label} />
        ) : null}
        {hasProgress ? (
          <div className="w-full">
            <div
              role="progressbar"
              aria-valuemin={0}
              aria-valuemax={100}
              aria-valuenow={clamped}
              aria-label={label}
              className="bg-muted h-1.5 w-full overflow-hidden rounded-full"
            >
              <div
                className="bg-primary h-full rounded-full"
                style={{ width: `${clamped}%` }}
              />
            </div>
            <Text as="p" size="caption" tone="muted" className="mt-2 text-center">
              {clamped}%
            </Text>
          </div>
        ) : null}
        {message ? (
          <Text as="p" size="small" tone="muted" className="text-center">
            {message}
          </Text>
        ) : null}
      </Stack>
    </div>
  );
}
