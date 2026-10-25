import { cn } from '@/lib/utils';

import { brandInk } from './brand-primitives';

export type BrandMarkProps = {
  className?: string;
  /** Decorative mark; pair with visible wordmark for a11y. */
  title?: string;
  /** When true, mark is decorative beside a visible wordmark (hide from AT). */
  decorative?: boolean;
};

/**
 * Geometric lockup — blueprint triangle over an institutional frame.
 */
export function BrandMark({
  className,
  title = 'AOVIAS',
  decorative = false,
}: BrandMarkProps) {
  return (
    <svg
      viewBox="0 0 40 40"
      role={decorative ? 'presentation' : 'img'}
      aria-hidden={decorative ? true : undefined}
      aria-label={decorative ? undefined : title}
      className={cn('text-primary size-9 shrink-0', className)}
    >
      <rect
        x="2"
        y="2"
        width="36"
        height="36"
        rx={brandInk.radiusLg}
        fill="currentColor"
        opacity={brandInk.fillFaint}
      />
      <rect
        x="2"
        y="2"
        width="36"
        height="36"
        rx={brandInk.radiusLg}
        fill="none"
        stroke="currentColor"
        strokeWidth={brandInk.stroke}
        opacity={brandInk.strokeMid}
      />
      <path
        d="M10 28 L20 10 L30 28"
        fill="none"
        stroke="currentColor"
        strokeWidth={brandInk.strokeStrong}
        strokeLinejoin="round"
      />
      <path
        d="M14.5 22 H25.5"
        fill="none"
        stroke="currentColor"
        strokeWidth={brandInk.strokeStrong}
        strokeLinecap="round"
        opacity={0.75}
      />
      <path
        d="M8 32 H32"
        fill="none"
        stroke="currentColor"
        strokeWidth={brandInk.strokeHair}
        opacity={brandInk.strokeMuted}
      />
    </svg>
  );
}
