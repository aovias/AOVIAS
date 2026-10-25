'use client';

import { useEffect, useState, type ReactNode } from 'react';

import { cn } from '@/lib/utils';

export type HeaderChromeProps = {
  children: ReactNode;
  className?: string;
};

/** Fixed enterprise header shell — transparency, blur, scroll elevation (AVLS n1/n2). */
export function HeaderChrome({ children, className }: HeaderChromeProps) {
  const [scrolled, setScrolled] = useState(false);

  useEffect(() => {
    const onScroll = () => {
      setScrolled(window.scrollY > 4);
    };

    onScroll();
    window.addEventListener('scroll', onScroll, { passive: true });
    return () => {
      window.removeEventListener('scroll', onScroll);
    };
  }, []);

  return (
    <header
      data-header-scrolled={scrolled ? 'true' : 'false'}
      className={cn(
        'fixed inset-x-0 top-0 z-50 border-b backdrop-blur-xl transition-[box-shadow,border-color,background-color,backdrop-filter] duration-[var(--duration-dropdown)] ease-[var(--easing-standard)] motion-reduce:transition-none',
        scrolled
          ? 'bg-nav-background/95 border-border shadow-raised'
          : 'bg-nav-background/85 border-border/40 shadow-none',
        'before:pointer-events-none before:absolute before:inset-0 before:-z-10 before:bg-gradient-to-b before:from-primary-muted/20 before:to-transparent before:opacity-0 before:transition-opacity before:duration-[var(--duration-dropdown)] motion-reduce:before:transition-none',
        'after:pointer-events-none after:absolute after:inset-x-0 after:bottom-0 after:h-px after:bg-gradient-to-r after:from-transparent after:via-primary/35 after:to-transparent after:opacity-0 after:transition-opacity after:duration-[var(--duration-dropdown)] motion-reduce:after:transition-none',
        scrolled && 'before:opacity-100 after:opacity-100',
        className,
      )}
    >
      {children}
    </header>
  );
}
