import Link from 'next/link';

import { buttonVariants } from '@/components/primitives';
import { cn } from '@/lib/utils';

/**
 * Pagination — accessible page navigation (links or controlled buttons).
 */
export type PaginationProps = {
  currentPage: number;
  totalPages: number;
  /** Build href for a page (preferred for server navigation). */
  getPageHref?: (page: number) => string;
  /** Controlled callback when href builder is omitted. */
  onPageChange?: (page: number) => void;
  className?: string;
  /** Show fewer page buttons on small screens (CSS). */
  siblingCount?: number;
};

function pageList(
  current: number,
  total: number,
  siblingCount: number,
): (number | 'ellipsis')[] {
  if (total <= 7) {
    return Array.from({ length: total }, (_, i) => i + 1);
  }
  const pages = new Set<number>();
  pages.add(1);
  pages.add(total);
  for (
    let i = Math.max(2, current - siblingCount);
    i <= Math.min(total - 1, current + siblingCount);
    i += 1
  ) {
    pages.add(i);
  }
  const sorted = [...pages].sort((a, b) => a - b);
  const result: (number | 'ellipsis')[] = [];
  for (let i = 0; i < sorted.length; i += 1) {
    const page = sorted[i]!;
    const prev = sorted[i - 1];
    if (prev !== undefined && page - prev > 1) result.push('ellipsis');
    result.push(page);
  }
  return result;
}

export function Pagination({
  currentPage,
  totalPages,
  getPageHref,
  onPageChange,
  className,
  siblingCount = 1,
}: PaginationProps) {
  if (totalPages <= 1) return null;

  const pages = pageList(currentPage, totalPages, siblingCount);
  const canPrev = currentPage > 1;
  const canNext = currentPage < totalPages;

  const renderControl = (
    page: number,
    label: string,
    options?: { disabled?: boolean; current?: boolean; className?: string },
  ) => {
    const disabled = options?.disabled ?? false;
    const current = options?.current ?? false;
    const classes = cn(
      buttonVariants({
        variant: current ? 'primary' : 'outline',
        size: 'sm',
      }),
      'min-w-9',
      options?.className,
      disabled && 'pointer-events-none opacity-50',
    );

    if (getPageHref && !disabled) {
      return (
        <Link
          href={getPageHref(page)}
          className={classes}
          aria-label={label}
          aria-current={current ? 'page' : undefined}
        >
          {label.match(/^\d+$/) ? label : label}
        </Link>
      );
    }

    return (
      <button
        type="button"
        className={classes}
        disabled={disabled}
        aria-label={label}
        aria-current={current ? 'page' : undefined}
        onClick={() => onPageChange?.(page)}
      >
        {label.match(/^\d+$/) ? label : label}
      </button>
    );
  };

  return (
    <nav
      aria-label="Pagination"
      className={cn('flex flex-wrap items-center justify-center gap-2', className)}
    >
      {renderControl(currentPage - 1, 'Previous', {
        disabled: !canPrev,
        className: 'px-3',
      })}

      <ul className="flex flex-wrap items-center gap-1">
        {pages.map((entry, index) =>
          entry === 'ellipsis' ? (
            <li
              key={`ellipsis-${index}`}
              className="text-muted-foreground px-1 text-small"
              aria-hidden="true"
            >
              …
            </li>
          ) : (
            <li key={entry} className="hidden sm:block">
              {renderControl(entry, String(entry), {
                current: entry === currentPage,
              })}
            </li>
          ),
        )}
        {/* Mobile: current page indicator */}
        <li className="sm:hidden">
          <span className="text-muted-foreground text-small px-2">
            Page {currentPage} of {totalPages}
          </span>
        </li>
      </ul>

      {renderControl(currentPage + 1, 'Next', {
        disabled: !canNext,
        className: 'px-3',
      })}
    </nav>
  );
}
