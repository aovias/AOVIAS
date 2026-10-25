import type { StateIllustration } from '@/components/system-states/types';
import { Text } from '@/components/primitives';
import { cn } from '@/lib/utils';

type IllustrationPlaceholderProps = {
  illustration?: StateIllustration;
  className?: string;
};

/**
 * Dashed illustration slot for system states (no decorative stock art required).
 */
export function IllustrationPlaceholder({
  illustration,
  className,
}: IllustrationPlaceholderProps) {
  if (illustration?.node) {
    return (
      <div className={cn('text-muted-foreground', className)}>
        {illustration.node}
      </div>
    );
  }

  const label = illustration?.label ?? 'Illustration';

  return (
    <div
      role="img"
      aria-label={label}
      className={cn(
        'border-border bg-muted/40 text-muted-foreground flex h-28 w-full max-w-xs items-center justify-center rounded-lg border border-dashed',
        className,
      )}
    >
      <Text as="span" size="caption">
        {label}
      </Text>
    </div>
  );
}
