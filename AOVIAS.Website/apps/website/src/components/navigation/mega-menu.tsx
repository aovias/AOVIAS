'use client';

import Link from 'next/link';
import { useCallback, useEffect, useId, useRef, useState } from 'react';
import { AnimatePresence, motion, useReducedMotion } from 'framer-motion';

import type { MegaMenuColumn, MegaMenuConfig } from '@/config/navigation';
import { motionTransitions } from '@/lib/motion/transitions';
import { getNavIcon } from '@/lib/navigation/icons';
import { useFocusTrap } from '@/lib/navigation/use-focus-trap';
import { cn } from '@/lib/utils';

const HOVER_OPEN_DELAY_MS = 90;
const HOVER_CLOSE_DELAY_MS = 180;

function MegaMenuColumnList({
  column,
  onClose,
  orientation = 'vertical',
}: {
  column: MegaMenuColumn;
  onClose: () => void;
  orientation?: 'vertical' | 'horizontal';
}) {
  const horizontal = orientation === 'horizontal';
  const items = column.items.filter((item) => !item.hidden);

  return (
    <div>
      <p className="text-muted-foreground mb-3 text-xs font-semibold tracking-wide uppercase">
        {column.title}
      </p>
      <ul
        className={cn(
          horizontal
            ? 'flex flex-row flex-wrap items-stretch gap-2'
            : 'flex flex-col gap-1',
        )}
      >
        {items.map((item) => {
          const Icon = getNavIcon(item.icon);
          return (
            <li key={item.id} className={cn(horizontal && 'min-w-[9.5rem] flex-1')}>
              <Link
                href={item.href}
                target={item.external ? '_blank' : undefined}
                rel={item.external ? 'noreferrer' : undefined}
                className={cn(
                  'group hover:bg-primary-muted/40 focus-visible:ring-ring flex rounded-lg px-2 py-2 transition-colors duration-[var(--duration-hover)] ease-[var(--easing-standard)] focus-visible:ring-2 focus-visible:outline-none',
                  horizontal ? 'h-full flex-col gap-1.5' : 'gap-3',
                )}
                onClick={onClose}
              >
                {Icon ? (
                  <Icon
                    className={cn(
                      'text-muted-foreground group-hover:text-primary size-4 shrink-0 transition-colors',
                      !horizontal && 'mt-0.5',
                    )}
                    aria-hidden="true"
                  />
                ) : null}
                <span className="min-w-0">
                  <span className="text-foreground block text-small font-medium">
                    {item.label}
                  </span>
                  {item.description && !horizontal ? (
                    <span className="text-muted-foreground mt-0.5 block text-xs">
                      {item.description}
                    </span>
                  ) : null}
                </span>
              </Link>
            </li>
          );
        })}
      </ul>
    </div>
  );
}

export type MegaMenuPanelProps = {
  config: MegaMenuConfig;
  open: boolean;
  onClose: () => void;
  labelledBy: string;
  /** When true, move focus into the panel (keyboard-opened menus). */
  trapFocus?: boolean;
  className?: string;
};

export function MegaMenuPanel({
  config,
  open,
  onClose,
  labelledBy,
  trapFocus = false,
  className,
}: MegaMenuPanelProps) {
  const panelRef = useRef<HTMLDivElement>(null);
  const panelDomId = `${config.id}-panel`;

  const handleEscape = useCallback(() => {
    onClose();
    document.getElementById(labelledBy)?.focus();
  }, [labelledBy, onClose]);

  useFocusTrap(panelRef, open && trapFocus, handleEscape);

  useEffect(() => {
    if (!open) return;

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape' && !trapFocus) {
        event.preventDefault();
        handleEscape();
      }
    };

    const onPointerDown = (event: MouseEvent) => {
      const target = event.target as Node | null;
      if (!panelRef.current || !target) return;
      if (panelRef.current.contains(target)) return;
      const trigger = document.getElementById(labelledBy);
      if (trigger?.contains(target)) return;
      onClose();
    };

    document.addEventListener('keydown', onKeyDown);
    document.addEventListener('mousedown', onPointerDown);
    return () => {
      document.removeEventListener('keydown', onKeyDown);
      document.removeEventListener('mousedown', onPointerDown);
    };
  }, [open, onClose, labelledBy, trapFocus, handleEscape]);

  const reduceMotion = useReducedMotion();
  const panelTransition = reduceMotion
    ? motionTransitions.reduced
    : motionTransitions.dropdown;

  return (
    <AnimatePresence>
      {open ? (
        <motion.div
          key={panelDomId}
          id={panelDomId}
          ref={panelRef}
          role="region"
          aria-labelledby={labelledBy}
          initial={reduceMotion ? false : { opacity: 0, y: -6 }}
          animate={{ opacity: 1, y: 0 }}
          exit={{ opacity: 0, y: -4 }}
          transition={panelTransition}
          className={cn(
            'absolute top-full left-0 z-40 pt-2.5',
            config.columns.some((column) => column.orientation === 'horizontal') ||
            config.columns.length > 3
              ? 'w-[min(100vw-2rem,64rem)]'
              : config.columns.length > 2
                ? 'w-[min(100vw-2rem,52rem)]'
                : 'w-[min(100vw-2rem,36rem)]',
            className,
          )}
        >
          <div className="border-border bg-nav-mega-background/95 shadow-floating relative overflow-hidden rounded-xl border p-5 backdrop-blur-xl before:pointer-events-none before:absolute before:inset-x-0 before:top-0 before:h-0.5 before:bg-gradient-to-r before:from-transparent before:via-primary/55 before:to-transparent sm:p-6">
        {(() => {
          const verticalColumns = config.columns.filter(
            (column) => column.orientation !== 'horizontal',
          );
          const horizontalColumns = config.columns.filter(
            (column) => column.orientation === 'horizontal',
          );
          const verticalCount = verticalColumns.length;

          return (
            <>
              {verticalCount > 0 ? (
                <div
                  className={cn(
                    'grid gap-6 lg:gap-8',
                    verticalCount >= 4
                      ? 'sm:grid-cols-2 xl:grid-cols-4'
                      : verticalCount === 3
                        ? 'sm:grid-cols-2 lg:grid-cols-3'
                        : verticalCount === 2
                          ? 'grid-cols-1 sm:grid-cols-2'
                          : 'grid-cols-1',
                  )}
                >
                  {verticalColumns.map((column) => (
                    <MegaMenuColumnList
                      key={column.id}
                      column={column}
                      onClose={onClose}
                    />
                  ))}
                </div>
              ) : null}

              {horizontalColumns.map((column) => (
                <div
                  key={column.id}
                  className={cn(verticalCount > 0 && 'border-border mt-6 border-t pt-5')}
                >
                  <MegaMenuColumnList
                    column={column}
                    onClose={onClose}
                    orientation="horizontal"
                  />
                </div>
              ))}
            </>
          );
        })()}

        {config.cta ? (
          <div className="border-border mt-6 border-t pt-4">
            <Link
              href={config.cta.href}
              className="hover:bg-primary-muted/40 focus-visible:ring-ring block rounded-lg px-3 py-2.5 transition-colors focus-visible:ring-2 focus-visible:outline-none"
              onClick={onClose}
            >
              <span className="text-foreground text-small font-medium">
                {config.cta.label}
              </span>
              {config.cta.description ? (
                <span className="text-muted-foreground mt-0.5 block text-xs">
                  {config.cta.description}
                </span>
              ) : null}
            </Link>
          </div>
        ) : null}
          </div>
        </motion.div>
      ) : null}
    </AnimatePresence>
  );
}

export type MegaMenuTriggerProps = {
  itemId: string;
  label: string;
  href: string;
  megaMenu: MegaMenuConfig;
  open: boolean;
  active: boolean;
  onOpen: (itemId: string) => void;
  onClose: () => void;
};

export function MegaMenuTrigger({
  itemId,
  label,
  href,
  megaMenu,
  open,
  active,
  onOpen,
  onClose,
}: MegaMenuTriggerProps) {
  const buttonId = useId();
  const [keyboardOpen, setKeyboardOpen] = useState(false);
  const openTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const closeTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const clearTimers = useCallback(() => {
    if (openTimerRef.current) {
      clearTimeout(openTimerRef.current);
      openTimerRef.current = null;
    }
    if (closeTimerRef.current) {
      clearTimeout(closeTimerRef.current);
      closeTimerRef.current = null;
    }
  }, []);

  const close = useCallback(() => {
    clearTimers();
    setKeyboardOpen(false);
    onClose();
  }, [clearTimers, onClose]);

  useEffect(() => () => clearTimers(), [clearTimers]);

  const scheduleOpen = useCallback(() => {
    clearTimers();
    setKeyboardOpen(false);
    openTimerRef.current = setTimeout(() => {
      onOpen(itemId);
      openTimerRef.current = null;
    }, HOVER_OPEN_DELAY_MS);
  }, [clearTimers, itemId, onOpen]);

  const scheduleClose = useCallback(() => {
    clearTimers();
    closeTimerRef.current = setTimeout(() => {
      setKeyboardOpen(false);
      onClose();
      closeTimerRef.current = null;
    }, HOVER_CLOSE_DELAY_MS);
  }, [clearTimers, onClose]);

  return (
    <div
      className="relative flex h-full items-center"
      onMouseEnter={scheduleOpen}
      onMouseLeave={scheduleClose}
    >
      <div className="inline-flex h-9 items-center">
        <Link
          href={href}
          className={cn(
            'focus-visible:ring-focus-ring relative inline-flex h-9 items-center rounded-sm ps-2.5 pe-1 text-small font-medium whitespace-nowrap transition-colors focus-visible:ring-2 focus-visible:outline-none',
            active || open
              ? 'text-nav-foreground-active after:bg-nav-indicator after:absolute after:inset-x-2 after:bottom-1 after:h-0.5 after:rounded-full'
              : 'text-nav-foreground hover:text-nav-foreground-hover',
          )}
          aria-current={active ? 'page' : undefined}
        >
          {label}
        </Link>
        <button
          id={buttonId}
          type="button"
          className={cn(
            'focus-visible:ring-focus-ring text-nav-foreground hover:text-nav-foreground-hover inline-flex h-9 w-7 shrink-0 items-center justify-center rounded-sm text-caption leading-none focus-visible:ring-2 focus-visible:outline-none',
            open && 'text-nav-foreground-active',
          )}
          aria-expanded={open}
          aria-controls={`${megaMenu.id}-panel`}
          aria-haspopup="dialog"
          aria-label={`${label} menu`}
          onClick={() => {
            clearTimers();
            if (open) {
              close();
            } else {
              setKeyboardOpen(true);
              onOpen(itemId);
            }
          }}
          onKeyDown={(event) => {
            if (event.key === 'ArrowDown') {
              event.preventDefault();
              clearTimers();
              setKeyboardOpen(true);
              onOpen(itemId);
            }
          }}
        >
          <span aria-hidden="true" className="text-[0.65rem] leading-none">
            ▾
          </span>
        </button>
      </div>
      <MegaMenuPanel
        config={megaMenu}
        open={open}
        onClose={close}
        labelledBy={buttonId}
        trapFocus={keyboardOpen && open}
      />
    </div>
  );
}
