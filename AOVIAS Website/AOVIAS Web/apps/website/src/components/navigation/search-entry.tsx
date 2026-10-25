'use client';

import dynamic from 'next/dynamic';
import { Search } from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
import { createPortal } from 'react-dom';

import { cn } from '@/lib/utils';

const EnterpriseSearchOverlay = dynamic(
  () =>
    import('@/components/search/enterprise-search-overlay').then(
      (mod) => mod.EnterpriseSearchOverlay,
    ),
  { ssr: false },
);

export type SearchEntryProps = {
  className?: string;
  /** Compact trigger for dense chrome (mobile drawer). */
  variant?: 'icon' | 'button';
  /** Register ⌘/Ctrl+K — keep true on a single header instance only. */
  enableHotkey?: boolean;
  /** Called when the dialog opens (e.g. close the mobile drawer first). */
  onOpen?: () => void;
};

/**
 * Global search entry — Enterprise Search overlay (Ctrl/⌘+K).
 * Overlay (and Framer) load only when opened — keeps mobile LCP clean (V11).
 */
export function SearchEntry({
  className,
  variant = 'icon',
  enableHotkey = true,
  onOpen,
}: SearchEntryProps) {
  const [open, setOpen] = useState(false);
  const [mounted, setMounted] = useState(false);

  const close = useCallback(() => {
    setOpen(false);
  }, []);

  const openSearch = useCallback(() => {
    onOpen?.();
    setOpen(true);
  }, [onOpen]);

  useEffect(() => {
    setMounted(true);
  }, []);

  useEffect(() => {
    if (!enableHotkey) return;

    const onKeyDown = (event: KeyboardEvent) => {
      if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === 'k') {
        event.preventDefault();
        openSearch();
      }
    };

    document.addEventListener('keydown', onKeyDown);
    return () => {
      document.removeEventListener('keydown', onKeyDown);
    };
  }, [enableHotkey, openSearch]);

  return (
    <>
      <button
        type="button"
        className={cn(
          'focus-visible:ring-ring inline-flex shrink-0 items-center self-center transition-colors focus-visible:ring-2 focus-visible:outline-none',
          variant === 'icon' &&
            'text-nav-foreground hover:text-nav-foreground-hover size-9 justify-center rounded-md',
          variant === 'button' &&
            'text-muted-foreground hover:text-foreground inline-flex h-9 items-center justify-center gap-2 rounded-md px-3 text-small font-medium',
          className,
        )}
        aria-label={variant === 'button' ? 'Search' : 'Open enterprise search'}
        aria-haspopup="dialog"
        aria-expanded={open}
        onClick={openSearch}
      >
        {variant === 'icon' ? (
          <Search className="size-4 shrink-0" aria-hidden="true" />
        ) : (
          'Search'
        )}
      </button>

      {mounted && open
        ? createPortal(
            <EnterpriseSearchOverlay open={open} onClose={close} />,
            document.body,
          )
        : null}
    </>
  );
}
