'use client';

import { ChevronDown } from 'lucide-react';
import { useId, useMemo, useState } from 'react';

import type { FaqItem } from '@/components/trust/models';
import { Input, Stack, Text } from '@/components/primitives';
import { cn } from '@/lib/utils';

/**
 * FAQ — accessible accordion from FaqItem models.
 * Search-ready: optional filter input over question/answer/keywords.
 */
export type FaqProps = {
  items: readonly FaqItem[];
  /** Show category headings when items include categories. */
  groupByCategory?: boolean;
  /** Enable client-side filter field. */
  searchable?: boolean;
  /** search input placeholder (UI chrome only). */
  searchPlaceholder?: string;
  className?: string;
  /** Allow multiple panels open (default true). */
  allowMultiple?: boolean;
};

export function Faq({
  items,
  groupByCategory = false,
  searchable = false,
  searchPlaceholder = 'Filter questions…',
  className,
  allowMultiple = true,
}: FaqProps) {
  const baseId = useId();
  const [query, setQuery] = useState('');
  const [openIds, setOpenIds] = useState<ReadonlySet<string>>(() => new Set());

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) return items;
    return items.filter((item) => {
      const haystack = [
        item.question,
        item.answer,
        item.category ?? '',
        ...(item.keywords ?? []),
      ]
        .join(' ')
        .toLowerCase();
      return haystack.includes(q);
    });
  }, [items, query]);

  const groups = useMemo(() => {
    if (!groupByCategory) {
      return [{ key: 'all', label: null as string | null, items: filtered }];
    }
    const map = new Map<string, FaqItem[]>();
    for (const item of filtered) {
      const key = item.category?.trim() || 'General';
      const list = map.get(key) ?? [];
      list.push(item);
      map.set(key, list);
    }
    return Array.from(map.entries()).map(([label, groupItems]) => ({
      key: label,
      label,
      items: groupItems,
    }));
  }, [filtered, groupByCategory]);

  const toggle = (id: string) => {
    setOpenIds((prev) => {
      const next = new Set(allowMultiple ? prev : []);
      if (prev.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  return (
    <div className={cn('w-full', className)}>
      <Stack gap={6}>
        {searchable ? (
          <div>
            <label className="sr-only" htmlFor={`${baseId}-search`}>
              Filter FAQ
            </label>
            <Input
              id={`${baseId}-search`}
              type="search"
              value={query}
              onChange={(event) => {
                setQuery(event.target.value);
              }}
              placeholder={searchPlaceholder}
            />
          </div>
        ) : null}

        <div role="status" aria-live="polite" className="sr-only">
          {filtered.length === 0
            ? 'No matching questions.'
            : `${filtered.length} question${filtered.length === 1 ? '' : 's'} shown.`}
        </div>
        {filtered.length === 0 ? (
          <Text tone="muted" size="small">
            No matching questions.
          </Text>
        ) : null}

        {groups.map((group) => (
          <div key={group.key}>
            {group.label ? (
              <Text
                as="p"
                weight="semibold"
                size="small"
                className="text-foreground mb-3"
              >
                {group.label}
              </Text>
            ) : null}
            <div className="border-border divide-border divide-y overflow-hidden rounded-lg border">
              {group.items.map((item) => {
                const panelId = `${baseId}-${item.id}-panel`;
                const buttonId = `${baseId}-${item.id}-button`;
                const isOpen = openIds.has(item.id);

                return (
                  <div
                    key={item.id}
                    className={cn(isOpen && 'bg-muted/25')}
                  >
                    <h3 className="m-0">
                      <button
                        id={buttonId}
                        type="button"
                        className={cn(
                          'focus-visible:ring-ring text-foreground flex w-full items-center justify-between gap-3 px-4 py-3.5 text-left text-small font-medium transition-colors duration-[var(--duration-hover)] ease-[var(--easing-standard)] focus-visible:ring-2 focus-visible:outline-none',
                          isOpen
                            ? 'bg-muted/40'
                            : 'hover:bg-muted/35',
                        )}
                        aria-expanded={isOpen}
                        aria-controls={panelId}
                        onClick={() => {
                          toggle(item.id);
                        }}
                      >
                        <span className="min-w-0 flex-1 leading-snug">
                          {item.question}
                        </span>
                        <ChevronDown
                          aria-hidden="true"
                          className={cn(
                            'text-muted-foreground size-4 shrink-0 transition-transform duration-[var(--duration-hover)] ease-[var(--easing-standard)] motion-reduce:transition-none',
                            isOpen && 'text-foreground rotate-180',
                          )}
                        />
                      </button>
                    </h3>
                    <div
                      id={panelId}
                      role="region"
                      aria-labelledby={buttonId}
                      aria-hidden={!isOpen}
                      className={cn(
                        'grid transition-[grid-template-rows] duration-300 ease-[var(--easing-standard)] motion-reduce:transition-none',
                        isOpen ? 'grid-rows-[1fr]' : 'grid-rows-[0fr]',
                      )}
                    >
                      <div className="overflow-hidden">
                        <div
                          className={cn(
                            'border-border/60 bg-muted/20 border-t px-4 pt-3 pb-4',
                            !isOpen && 'pointer-events-none',
                          )}
                        >
                          <Text as="p" size="small" tone="muted">
                            {item.answer}
                          </Text>
                        </div>
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        ))}
      </Stack>
    </div>
  );
}
