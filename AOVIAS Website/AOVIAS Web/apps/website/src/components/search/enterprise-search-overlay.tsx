'use client';

import { Search, X } from 'lucide-react';
import { useRouter } from 'next/navigation';
import {
  useCallback,
  useEffect,
  useId,
  useMemo,
  useRef,
  useState,
  type KeyboardEvent as ReactKeyboardEvent,
} from 'react';
import { AnimatePresence, motion, useReducedMotion } from 'framer-motion';

import { useBodyScrollLock, useFocusTrap } from '@/lib/navigation/use-focus-trap';
import { motionDelays } from '@/lib/motion/durations';
import { motionTransitions } from '@/lib/motion/transitions';
import { cn } from '@/lib/utils';
import {
  getSearchCategoryLabel,
  groupSearchResults,
  runEnterpriseSearch,
  type EnterpriseSearchResult,
  type SearchCategory,
} from '@/registries/avlf';

export type EnterpriseSearchOverlayProps = {
  open: boolean;
  onClose: () => void;
};

const CATEGORY_ORDER: SearchCategory[] = [
  'products',
  'technology',
  'industries',
  'resources',
  'company',
  'legal',
  'pricing',
  'contact',
];

/**
 * Global Enterprise Search overlay — Ctrl/⌘+K command palette.
 * Backed by static registry; provider-swappable for Release 2.
 */
export function EnterpriseSearchOverlay({
  open,
  onClose,
}: EnterpriseSearchOverlayProps) {
  const router = useRouter();
  const reduceMotion = useReducedMotion();
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<EnterpriseSearchResult[]>([]);
  const [activeIndex, setActiveIndex] = useState(0);
  const dialogRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const listRef = useRef<HTMLDivElement>(null);
  const titleId = useId();
  const descriptionId = useId();
  const inputId = useId();
  const listboxId = useId();

  useFocusTrap(dialogRef, open, onClose);
  useBodyScrollLock(open);

  useEffect(() => {
    if (!open) {
      setQuery('');
      setResults([]);
      setActiveIndex(0);
      return;
    }
    inputRef.current?.focus();
    void runEnterpriseSearch('').then((items) => {
      setResults(items.slice(0, 24));
      setActiveIndex(0);
    });
  }, [open]);

  useEffect(() => {
    if (!open) return;
    let cancelled = false;
    const handle = window.setTimeout(() => {
      void runEnterpriseSearch(query).then((items) => {
        if (cancelled) return;
        setResults(query.trim() ? items : items.slice(0, 24));
        setActiveIndex(0);
      });
    }, 80);
    return () => {
      cancelled = true;
      window.clearTimeout(handle);
    };
  }, [query, open]);

  const flatResults = results;
  const grouped = useMemo(() => groupSearchResults(flatResults), [flatResults]);

  const navigateTo = useCallback(
    (href: string) => {
      onClose();
      router.push(href);
    },
    [onClose, router],
  );

  const openKnowledgeFallback = useCallback(() => {
    const q = query.trim();
    const href = q
      ? `/resources/search?q=${encodeURIComponent(q)}`
      : '/resources/search';
    navigateTo(href);
  }, [navigateTo, query]);

  const onKeyDown = useCallback(
    (event: ReactKeyboardEvent<HTMLInputElement>) => {
      if (event.key === 'ArrowDown') {
        event.preventDefault();
        setActiveIndex((index) =>
          flatResults.length === 0 ? 0 : (index + 1) % flatResults.length,
        );
        return;
      }
      if (event.key === 'ArrowUp') {
        event.preventDefault();
        setActiveIndex((index) =>
          flatResults.length === 0
            ? 0
            : (index - 1 + flatResults.length) % flatResults.length,
        );
        return;
      }
      if (event.key === 'Enter') {
        event.preventDefault();
        const selected = flatResults[activeIndex];
        if (selected) {
          navigateTo(selected.href);
          return;
        }
        openKnowledgeFallback();
        return;
      }
      if (event.key === 'Escape') {
        event.preventDefault();
        onClose();
      }
    },
    [
      activeIndex,
      flatResults,
      navigateTo,
      onClose,
      openKnowledgeFallback,
    ],
  );

  useEffect(() => {
    if (!open || !listRef.current) return;
    const active = listRef.current.querySelector<HTMLElement>(
      `[data-search-index="${activeIndex}"]`,
    );
    active?.scrollIntoView({ block: 'nearest' });
  }, [activeIndex, open]);

  const enterTransition = reduceMotion
    ? motionTransitions.reduced
    : motionTransitions.overlay;
  const exitTransition = reduceMotion
    ? motionTransitions.reduced
    : motionTransitions.overlayExit;

  let runningIndex = 0;

  return (
    <AnimatePresence>
      {open ? (
        <div className="fixed inset-0 z-[80] flex items-start justify-center px-4 pt-[10vh] sm:pt-[12vh]">
          <motion.div
            className="bg-background/70 absolute inset-0 backdrop-blur-sm"
            aria-hidden="true"
            initial={reduceMotion ? false : { opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            transition={exitTransition}
            onClick={onClose}
          />
          <motion.div
            ref={dialogRef}
            role="dialog"
            aria-modal="true"
            aria-labelledby={titleId}
            aria-describedby={descriptionId}
            initial={reduceMotion ? false : { opacity: 0, y: -10, scale: 0.98 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: -6, scale: 0.98 }}
            transition={enterTransition}
            className="border-border bg-surface-elevated shadow-floating relative z-10 flex w-full max-w-xl flex-col overflow-hidden rounded-xl border"
          >
            <div className="via-primary/45 absolute inset-x-0 top-0 h-px bg-gradient-to-r from-transparent to-transparent" />

            <div className="border-border flex items-center gap-3 border-b px-4 py-3">
              <Search
                className="text-muted-foreground size-4 shrink-0"
                aria-hidden="true"
              />
              <label className="sr-only" htmlFor={inputId}>
                Search AOVIAS
              </label>
              <input
                ref={inputRef}
                id={inputId}
                type="search"
                value={query}
                onChange={(event) => {
                  setQuery(event.target.value);
                }}
                onKeyDown={onKeyDown}
                placeholder="Search products, technology, resources…"
                className="text-ink placeholder:text-input-placeholder w-full bg-transparent text-small outline-none"
                aria-autocomplete="list"
                aria-controls={listboxId}
                aria-activedescendant={
                  flatResults[activeIndex]
                    ? `${listboxId}-option-${activeIndex}`
                    : undefined
                }
                autoComplete="off"
                spellCheck={false}
              />
              <kbd className="text-ink-muted border-border text-caption hidden rounded-sm border px-1.5 py-0.5 font-medium sm:inline">
                esc
              </kbd>
              <button
                type="button"
                className="text-nav-foreground hover:text-nav-foreground-hover focus-visible:ring-focus-ring inline-flex size-8 items-center justify-center rounded-md focus-visible:ring-2 focus-visible:outline-none"
                aria-label="Close search"
                onClick={onClose}
              >
                <X className="size-4" aria-hidden="true" />
              </button>
            </div>

            <div className="px-4 pt-3 pb-2">
              <h2 id={titleId} className="sr-only">
                Enterprise search
              </h2>
              <p id={descriptionId} className="text-ink-muted text-caption">
                Navigate AOVIAS — products, technology, industries, resources, and
                more. Use ↑↓ and Enter.
              </p>
            </div>

            <div
              ref={listRef}
              id={listboxId}
              role="listbox"
              aria-label="Search results"
              className="max-h-[min(52vh,24rem)] overflow-y-auto px-2 pb-3"
            >
              {flatResults.length === 0 ? (
                <div className="text-ink-muted px-3 py-8 text-center text-small">
                  <p>No matching destinations.</p>
                  <button
                    type="button"
                    className="text-primary mt-3 font-medium underline-offset-2 hover:underline"
                    onClick={openKnowledgeFallback}
                  >
                    Search knowledge platform
                    {query.trim() ? ` for “${query.trim()}”` : ''}
                  </button>
                </div>
              ) : (
                grouped.map((group) => {
                  const groupNodes = group.items.map((item) => {
                    const index = runningIndex;
                    runningIndex += 1;
                    const active = index === activeIndex;
                    return (
                      <button
                        key={item.id}
                        id={`${listboxId}-option-${index}`}
                        type="button"
                        role="option"
                        aria-selected={active}
                        data-search-index={index}
                        className={cn(
                          'focus-visible:ring-focus-ring flex w-full flex-col rounded-lg px-3 py-2.5 text-left transition-colors duration-[var(--duration-hover)] ease-[var(--easing-standard)] focus-visible:ring-2 focus-visible:outline-none',
                          active
                            ? 'bg-primary-muted/50 text-foreground'
                            : 'hover:bg-muted/70 text-foreground',
                        )}
                        style={
                          reduceMotion
                            ? undefined
                            : {
                                animation: `em-diagram-step var(--duration-overlay) var(--easing-enter) both`,
                                animationDelay: `${Math.min(index, 8) * motionDelays.diagramStep * 1000}ms`,
                              }
                        }
                        onMouseEnter={() => {
                          setActiveIndex(index);
                        }}
                        onClick={() => {
                          navigateTo(item.href);
                        }}
                      >
                        <span className="text-small font-medium">{item.title}</span>
                        {item.description ? (
                          <span className="text-muted-foreground mt-0.5 line-clamp-1 text-caption">
                            {item.description}
                          </span>
                        ) : null}
                      </button>
                    );
                  });

                  return (
                    <div key={group.category} className="mb-2">
                      <p className="text-muted-foreground px-3 py-1.5 text-caption font-semibold tracking-wide uppercase">
                        {group.label}
                      </p>
                      <div className="flex flex-col gap-0.5">{groupNodes}</div>
                    </div>
                  );
                })
              )}
            </div>

            <div className="border-border bg-surface-muted/40 text-muted-foreground flex items-center justify-between gap-3 border-t px-4 py-2 text-caption">
              <span className="hidden sm:inline">
                {CATEGORY_ORDER.map(getSearchCategoryLabel).join(' · ')}
              </span>
              <button
                type="button"
                className="hover:text-foreground ms-auto font-medium transition-colors"
                onClick={openKnowledgeFallback}
              >
                Open knowledge search →
              </button>
            </div>
          </motion.div>
        </div>
      ) : null}
    </AnimatePresence>
  );
}
