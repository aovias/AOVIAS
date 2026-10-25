import { cn } from '@/lib/utils';

import { CommandOperations } from './command-operations';
import { StudioDelivery } from './studio-delivery';

export type ProductPairProps = {
  className?: string;
  commandTitle?: string;
  studioTitle?: string;
};

/**
 * Side-by-side Command + Studio product visualization for marketing heroes.
 */
export function ProductPair({
  className,
  commandTitle = 'AOVIAS Command',
  studioTitle = 'AOVIAS Studio',
}: ProductPairProps) {
  return (
    <div
      className={cn(
        'grid min-w-0 gap-3 sm:grid-cols-2 sm:gap-4',
        className,
      )}
    >
      <CommandOperations
        title={commandTitle}
        className="min-h-[11rem] xl:min-h-[13rem] 2xl:min-h-[15rem]"
      />
      <StudioDelivery
        title={studioTitle}
        className="min-h-[11rem] xl:min-h-[13rem] 2xl:min-h-[15rem]"
      />
    </div>
  );
}
