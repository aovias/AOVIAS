'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import {
  useCallback,
  useEffect,
  useState,
  type ComponentType,
} from 'react';

import { mainNavigation, type MegaMenuConfig } from '@/config/navigation';
import { findBestActiveHref, isActiveHref } from '@/lib/navigation/active';
import { cn } from '@/lib/utils';

type MegaMenuTriggerProps = {
  itemId: string;
  label: string;
  href: string;
  megaMenu: MegaMenuConfig;
  open: boolean;
  active: boolean;
  onOpen: (itemId: string) => void;
  onClose: () => void;
};

export type MainNavProps = {
  className?: string;
};

/**
 * Desktop primary nav.
 * Mega-menu (Framer) is loaded only after idle on lg+ — keeps it out of the
 * initial homepage script graph for mobile Lighthouse (V11).
 */
export function MainNav({ className }: MainNavProps) {
  const pathname = usePathname();
  const [openId, setOpenId] = useState<string | null>(null);
  const [desktop, setDesktop] = useState(false);
  const [MegaMenuTrigger, setMegaMenuTrigger] = useState<ComponentType<
    MegaMenuTriggerProps
  > | null>(null);

  useEffect(() => {
    const mq = window.matchMedia('(min-width: 1024px)');
    const sync = () => {
      setDesktop(mq.matches);
    };
    sync();
    mq.addEventListener('change', sync);
    return () => {
      mq.removeEventListener('change', sync);
    };
  }, []);

  useEffect(() => {
    if (!desktop || MegaMenuTrigger) return;

    let cancelled = false;
    const load = () => {
      void import('@/components/navigation/mega-menu').then((mod) => {
        if (!cancelled) {
          setMegaMenuTrigger(() => mod.MegaMenuTrigger);
        }
      });
    };

    const idle = (
      window as Window & {
        requestIdleCallback?: (
          cb: () => void,
          opts?: { timeout: number },
        ) => number;
        cancelIdleCallback?: (id: number) => void;
      }
    ).requestIdleCallback;

    if (typeof idle === 'function') {
      const id = idle(load, { timeout: 2500 });
      return () => {
        cancelled = true;
        window.cancelIdleCallback?.(id);
      };
    }

    const timer = window.setTimeout(load, 1200);
    return () => {
      cancelled = true;
      window.clearTimeout(timer);
    };
  }, [desktop, MegaMenuTrigger]);

  const hrefs = mainNavigation.map((item) => item.href);
  const bestActive = findBestActiveHref(pathname, hrefs);

  const onOpen = useCallback((itemId: string) => {
    setOpenId(itemId);
  }, []);

  const onClose = useCallback(() => {
    setOpenId(null);
  }, []);

  return (
    <nav
      aria-label="Primary"
      className={cn('hidden h-full items-center gap-1 lg:flex', className)}
    >
      {mainNavigation
        .filter((item) => !item.hidden)
        .map((item) => {
          const active =
            bestActive === item.href || isActiveHref(pathname, item.href);

          if (item.megaMenu && desktop && MegaMenuTrigger) {
            return (
              <MegaMenuTrigger
                key={item.id}
                itemId={item.id}
                label={item.label}
                href={item.href}
                megaMenu={item.megaMenu}
                open={openId === item.id}
                active={active}
                onOpen={onOpen}
                onClose={onClose}
              />
            );
          }

          return (
            <Link
              key={item.id}
              href={item.href}
              className={cn(
                'focus-visible:ring-focus-ring relative inline-flex h-9 items-center rounded-sm px-2.5 text-small font-medium whitespace-nowrap transition-colors focus-visible:ring-2 focus-visible:outline-none',
                active
                  ? 'text-nav-foreground-active after:bg-nav-indicator after:absolute after:inset-x-2 after:bottom-1 after:h-0.5 after:rounded-full'
                  : 'text-nav-foreground hover:text-nav-foreground-hover',
              )}
              aria-current={active ? 'page' : undefined}
            >
              {item.label}
            </Link>
          );
        })}
    </nav>
  );
}
