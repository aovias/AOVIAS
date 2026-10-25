'use client';

import { useEffect, useState } from 'react';

import type { TocItem } from '@/components/content';
import { Text } from '@/components/primitives';
import { cn } from '@/lib/utils';

/**
 * TableOfContents — nested heading nav with active section tracking.
 */
export type TableOfContentsProps = {
  items: readonly TocItem[];
  className?: string;
  title?: string;
  /** Controlled active id; when omitted, IntersectionObserver tracks headings. */
  activeId?: string;
  sticky?: boolean;
};

function flattenIds(items: readonly TocItem[]): string[] {
  const ids: string[] = [];
  for (const item of items) {
    ids.push(item.id);
    if (item.children) ids.push(...flattenIds(item.children));
  }
  return ids;
}

function TocList({
  items,
  activeId,
  depth = 0,
}: {
  items: readonly TocItem[];
  activeId?: string;
  depth?: number;
}) {
  return (
    <ul className={cn('flex flex-col gap-1', depth > 0 && 'ms-3 border-s border-border ps-3')}>
      {items.map((item) => {
        const active = item.id === activeId;
        return (
          <li key={item.id}>
            <a
              href={item.href}
              className={cn(
                'focus-visible:ring-ring block rounded-sm px-2 py-1 text-small transition-colors focus-visible:ring-2 focus-visible:outline-none',
                active
                  ? 'bg-primary/10 text-foreground font-medium'
                  : 'text-muted-foreground hover:text-foreground',
              )}
              aria-current={active ? 'location' : undefined}
            >
              {item.title}
            </a>
            {item.children && item.children.length > 0 ? (
              <TocList
                items={item.children}
                {...(activeId !== undefined ? { activeId } : {})}
                depth={depth + 1}
              />
            ) : null}
          </li>
        );
      })}
    </ul>
  );
}

export function TableOfContents({
  items,
  className,
  title = 'On this page',
  activeId: controlledActiveId,
  sticky = true,
}: TableOfContentsProps) {
  const [observedId, setObservedId] = useState<string | undefined>();
  const activeId = controlledActiveId ?? observedId;

  useEffect(() => {
    if (controlledActiveId !== undefined) return;
    const ids = flattenIds(items);
    if (ids.length === 0) return;

    const elements = ids
      .map((id) => document.getElementById(id))
      .filter((el): el is HTMLElement => Boolean(el));

    if (elements.length === 0) return;

    const observer = new IntersectionObserver(
      (entries) => {
        const visible = entries
          .filter((entry) => entry.isIntersecting)
          .sort(
            (a, b) =>
              (a.target as HTMLElement).offsetTop -
              (b.target as HTMLElement).offsetTop,
          );
        const first = visible[0]?.target;
        if (first?.id) setObservedId(first.id);
      },
      {
        rootMargin: '-20% 0px -65% 0px',
        threshold: [0, 1],
      },
    );

    for (const el of elements) observer.observe(el);
    return () => observer.disconnect();
  }, [items, controlledActiveId]);

  if (items.length === 0) return null;

  return (
    <nav
      aria-label={title}
      className={cn(
        'border-border bg-card rounded-lg border p-4',
        sticky && 'lg:sticky lg:top-24',
        className,
      )}
    >
      <Text as="p" size="caption" weight="medium" tone="muted" className="mb-3">
        {title}
      </Text>
      <TocList
        items={items}
        {...(activeId !== undefined ? { activeId } : {})}
      />
    </nav>
  );
}
