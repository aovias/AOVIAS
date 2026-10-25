'use client';

import Link from 'next/link';
import { ChevronDown, Menu, X } from 'lucide-react';
import {
  useCallback,
  useEffect,
  useId,
  useRef,
  useState,
  type ReactNode,
} from 'react';
import { createPortal } from 'react-dom';

import { SearchEntry } from '@/components/navigation/search-entry';
import { UtilityNav } from '@/components/navigation/utility-nav';
import {
  mainNavigation,
  type MainNavItem,
  type NavItem,
} from '@/config/navigation';
import { getNavIcon } from '@/lib/navigation/icons';
import { useBodyScrollLock } from '@/lib/navigation/use-body-scroll-lock';
import { useFocusTrap } from '@/lib/navigation/use-focus-trap';
import { ThemeToggle } from '@/providers/theme';
import { cn } from '@/lib/utils';

export type MobileDrawerProps = {
  className?: string;
};

const touchControl =
  'inline-flex size-8 shrink-0 items-center justify-center rounded-md focus-visible:ring-2 focus-visible:outline-none focus-visible:ring-ring';

function NestedGroup({
  item,
  onNavigate,
}: {
  item: MainNavItem;
  onNavigate: () => void;
}) {
  const [expanded, setExpanded] = useState(false);
  const panelId = useId();

  const childItems: readonly NavItem[] =
    item.megaMenu?.columns.flatMap((column) => column.items) ??
    item.children ??
    [];

  if (childItems.length === 0) {
    return (
      <Link
        href={item.href}
        className="text-foreground hover:bg-muted focus-visible:ring-ring flex min-h-8 items-center rounded-lg px-3 py-2 text-small font-medium transition-colors duration-[var(--duration-hover)] ease-[var(--easing-standard)] focus-visible:ring-2 focus-visible:outline-none"
        onClick={onNavigate}
      >
        {item.label}
      </Link>
    );
  }

  return (
    <div className="border-border/60 rounded-lg border">
      <button
        type="button"
        className="text-foreground hover:bg-muted focus-visible:ring-ring flex min-h-8 w-full items-center justify-between gap-3 rounded-lg px-3 py-2 text-left text-small font-medium transition-colors duration-[var(--duration-hover)] ease-[var(--easing-standard)] focus-visible:ring-2 focus-visible:outline-none"
        aria-expanded={expanded}
        aria-controls={panelId}
        onClick={() => {
          setExpanded((value) => !value);
        }}
      >
        <span>{item.label}</span>
        <ChevronDown
          className={cn(
            'text-muted-foreground size-4 shrink-0 transition-transform duration-[var(--duration-hover)] ease-[var(--easing-standard)]',
            expanded && 'rotate-180',
          )}
          aria-hidden="true"
        />
      </button>
      {expanded ? (
        <ul
          id={panelId}
          className="border-border flex flex-col gap-0.5 border-t px-2 py-2"
        >
          <li>
            <Link
              href={item.href}
              className="text-foreground hover:bg-muted focus-visible:ring-ring flex min-h-8 items-center rounded-md px-3 py-2 text-small font-medium focus-visible:ring-2 focus-visible:outline-none"
              onClick={onNavigate}
            >
              Overview
            </Link>
          </li>
          {childItems
            .filter((child) => !child.hidden)
            .map((child) => {
              const Icon = getNavIcon(child.icon);
              return (
                <li key={child.id}>
                  <Link
                    href={child.href}
                    className="text-muted-foreground hover:text-foreground hover:bg-muted focus-visible:ring-ring flex min-h-8 items-center gap-2.5 rounded-md px-3 py-2 text-small transition-colors duration-[var(--duration-hover)] ease-[var(--easing-standard)] focus-visible:ring-2 focus-visible:outline-none"
                    onClick={onNavigate}
                  >
                    {Icon ? (
                      <Icon className="size-4 shrink-0" aria-hidden="true" />
                    ) : null}
                    <span className="min-w-0 leading-snug">{child.label}</span>
                  </Link>
                </li>
              );
            })}
        </ul>
      ) : null}
    </div>
  );
}

function DrawerPortal({ children }: { children: ReactNode }) {
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
  }, []);

  if (!mounted) return null;
  return createPortal(children, document.body);
}

/**
 * Mobile navigation drawer — portaled overlay, focus trap, ESC, scroll lock.
 * CSS transitions only (no Framer on the mobile critical path).
 */
export function MobileDrawer({ className }: MobileDrawerProps) {
  const [open, setOpen] = useState(false);
  const panelRef = useRef<HTMLDivElement>(null);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const titleId = useId();

  const close = useCallback(() => {
    setOpen(false);
  }, []);

  useFocusTrap(panelRef, open, close);
  useBodyScrollLock(open);

  useEffect(() => {
    if (open) return;
    // Restore focus to hamburger after close (focus trap also restores, but
    // portal unmount can race — prefer explicit trigger focus).
    if (document.activeElement === document.body) {
      triggerRef.current?.focus();
    }
  }, [open]);

  return (
    <div className={cn('lg:hidden', className)}>
      <button
        ref={triggerRef}
        type="button"
        className={cn(
          touchControl,
          'border-border bg-muted text-foreground border',
        )}
        aria-expanded={open}
        aria-controls="mobile-navigation-drawer"
        aria-haspopup="dialog"
        aria-label={open ? 'Close menu' : 'Open menu'}
        onClick={() => {
          setOpen((value) => !value);
        }}
      >
        {open ? (
          <X className="size-5" aria-hidden="true" />
        ) : (
          <Menu className="size-5" aria-hidden="true" />
        )}
      </button>

      {open ? (
        <DrawerPortal>
          <div
            className="em-drawer-root fixed inset-0 z-[100] lg:hidden"
            data-mobile-nav="open"
          >
            <button
              type="button"
              className="em-drawer-backdrop absolute inset-0 bg-black/55 backdrop-blur-sm"
              aria-label="Close menu"
              tabIndex={-1}
              onClick={close}
            />
            <div
              ref={panelRef}
              id="mobile-navigation-drawer"
              role="dialog"
              aria-modal="true"
              aria-labelledby={titleId}
              className={cn(
                'em-drawer-panel border-border bg-background text-foreground shadow-raised',
                'fixed inset-y-0 right-0 z-[101] flex w-[min(100vw-1.25rem,22rem)] flex-col border-l',
                'pt-[env(safe-area-inset-top)] pb-[env(safe-area-inset-bottom)]',
              )}
              style={{ backgroundColor: 'var(--background, #0b1220)' }}
            >
              <div className="border-border flex min-h-14 items-center justify-between gap-3 border-b px-4">
                <h2
                  id={titleId}
                  className="text-foreground text-small font-semibold tracking-tight"
                >
                  Menu
                </h2>
                <button
                  type="button"
                  className={cn(
                    touchControl,
                    'text-muted-foreground hover:text-foreground hover:bg-muted',
                  )}
                  aria-label="Close menu"
                  onClick={close}
                >
                  <X className="size-5" aria-hidden="true" />
                </button>
              </div>

              <div className="flex flex-1 flex-col gap-5 overflow-y-auto overscroll-contain p-4">
                <SearchEntry
                  variant="button"
                  enableHotkey={false}
                  onOpen={close}
                  className="border-border bg-muted/60 text-foreground hover:bg-muted w-full justify-start border"
                />

                <nav aria-label="Mobile primary">
                  <ul className="flex flex-col gap-2">
                    {mainNavigation
                      .filter((item) => !item.hidden)
                      .map((item) => (
                        <li key={item.id}>
                          <NestedGroup item={item} onNavigate={close} />
                        </li>
                      ))}
                  </ul>
                </nav>

                <div className="border-border mt-auto space-y-4 border-t pt-4">
                  <UtilityNav density="footer" />
                  <div className="flex h-8 items-center justify-between gap-3">
                    <span className="text-muted-foreground text-small">
                      Theme
                    </span>
                    <ThemeToggle />
                  </div>
                </div>
              </div>
            </div>
          </div>
        </DrawerPortal>
      ) : null}
    </div>
  );
}

/** @deprecated Prefer MobileDrawer — alias for Sprint 1.3 import stability. */
export function MobileNav(props: MobileDrawerProps) {
  return <MobileDrawer {...props} />;
}

export type MobileNavProps = MobileDrawerProps;
