'use client';

import { useEffect, useState } from 'react';

import { Container } from '@/components/primitives';
import { cn } from '@/lib/utils';

export type ProductNavItem = {
  id: string;
  label: string;
  /** Element id to scroll/observe. */
  href: string;
};

/**
 * ProductNavigation — sticky in-page section nav with active section tracking.
 */
export type ProductNavigationProps = {
  items: readonly ProductNavItem[];
  className?: string;
  /** Offset from top when sticky (e.g. under global header). */
  stickyOffsetClassName?: string;
};

export function ProductNavigation({
  items,
  className,
  stickyOffsetClassName = 'top-16',
}: ProductNavigationProps) {
  const [activeId, setActiveId] = useState<string | null>(items[0]?.href ?? null);

  useEffect(() => {
    const sectionIds = items.map((item) => item.href.replace(/^#/, ''));
    const elements = sectionIds
      .map((id) => document.getElementById(id))
      .filter((el): el is HTMLElement => Boolean(el));

    if (elements.length === 0) return;

    const observer = new IntersectionObserver(
      (entries) => {
        const visible = entries
          .filter((entry) => entry.isIntersecting)
          .sort((a, b) => b.intersectionRatio - a.intersectionRatio);
        const top = visible[0];
        if (top?.target.id) {
          setActiveId(`#${top.target.id}`);
        }
      },
      {
        rootMargin: '-20% 0px -55% 0px',
        threshold: [0.1, 0.25, 0.5],
      },
    );

    elements.forEach((el) => {
      observer.observe(el);
    });

    return () => {
      observer.disconnect();
    };
  }, [items]);

  return (
    <nav
      aria-label="Product sections"
      className={cn(
        'border-border bg-background/95 sticky z-20 border-b backdrop-blur',
        stickyOffsetClassName,
        className,
      )}
    >
      <Container width="wide" className="overflow-x-auto">
        <ul className="flex min-h-12 items-center gap-1 py-2">
          {items.map((item) => {
            const active = activeId === item.href;
            return (
              <li key={item.id}>
                <a
                  href={item.href}
                  className={cn(
                    'focus-visible:ring-ring text-small inline-flex h-9 items-center rounded-md px-3 font-medium whitespace-nowrap transition-colors focus-visible:ring-2 focus-visible:outline-none',
                    active
                      ? 'bg-muted text-foreground'
                      : 'text-muted-foreground hover:text-foreground hover:bg-muted/60',
                  )}
                  aria-current={active ? 'true' : undefined}
                >
                  {item.label}
                </a>
              </li>
            );
          })}
        </ul>
      </Container>
    </nav>
  );
}
