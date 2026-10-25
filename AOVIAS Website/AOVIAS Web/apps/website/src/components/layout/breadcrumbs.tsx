'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { ChevronRight } from 'lucide-react';

import {
  buildBreadcrumbsFromPath,
  type BreadcrumbItem,
} from '@/lib/navigation/breadcrumbs';
import { cn } from '@/lib/utils';

export type BreadcrumbsProps = {
  items?: readonly BreadcrumbItem[];
  className?: string;
};

export function Breadcrumbs({ items, className }: BreadcrumbsProps) {
  const pathname = usePathname();
  const resolved = items ?? buildBreadcrumbsFromPath(pathname);

  if (resolved.length === 0) return null;

  return (
    <nav aria-label="Breadcrumb" className={cn('mb-6', className)}>
      <ol className="text-breadcrumb-foreground text-caption flex flex-wrap items-center gap-1.5">
        {resolved.map((item, index) => {
          const isCurrent = item.current ?? index === resolved.length - 1;

          return (
            <li key={`${item.label}-${index}`} className="flex items-center gap-1.5">
              {index > 0 ? (
                <ChevronRight
                  aria-hidden="true"
                  className="size-3.5 shrink-0 opacity-60"
                />
              ) : null}
              {isCurrent || !item.href ? (
                <span
                  className="text-breadcrumb-current font-medium"
                  aria-current={isCurrent ? 'page' : undefined}
                >
                  {item.label}
                </span>
              ) : (
                <Link
                  href={item.href}
                  className="hover:text-nav-foreground-hover focus-visible:ring-focus-ring rounded-sm transition-colors focus-visible:ring-2 focus-visible:outline-none"
                >
                  {item.label}
                </Link>
              )}
            </li>
          );
        })}
      </ol>
    </nav>
  );
}

/** @deprecated Prefer Breadcrumbs — kept for import stability during Sprint 1.4. */
export function BreadcrumbPlaceholder(props: BreadcrumbsProps) {
  return <Breadcrumbs {...props} />;
}

export type { BreadcrumbItem };
