'use client';

import { useEffect, useState } from 'react';

import { useTheme } from './use-theme';

export type ThemeToggleProps = {
  className?: string;
};

/**
 * Accessible theme toggle with sun/moon icon and descriptive name.
 * Visual size stays compact for header chrome; hit area padded via padding.
 */
export function ThemeToggle({ className }: ThemeToggleProps) {
  const { resolvedTheme, toggle } = useTheme();
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
  }, []);

  const label = !mounted
    ? 'Toggle color theme'
    : resolvedTheme === 'dark'
      ? 'Switch to light theme'
      : 'Switch to dark theme';

  // Compact visual (size-6 = 32px in AOVIAS spacing tokens).
  // Do NOT use size-11 — in this theme size-11 maps to --space-11 (96px).
  const classes = [
    'inline-flex size-6 shrink-0 items-center justify-center',
    'rounded-md border border-border bg-muted',
    'text-foreground',
    'cursor-pointer',
    'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring',
    className,
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <button type="button" onClick={toggle} aria-label={label} title={label} className={classes}>
      <span className="sr-only">{label}</span>
      {mounted && resolvedTheme === 'dark' ? (
        <svg
          viewBox="0 0 24 24"
          className="size-4 scale-110"
          aria-hidden="true"
          fill="none"
          stroke="currentColor"
          strokeWidth="1.75"
        >
          <circle cx="12" cy="12" r="4" />
          <path d="M12 2v2M12 20v2M4.93 4.93l1.41 1.41M17.66 17.66l1.41 1.41M2 12h2M20 12h2M4.93 19.07l1.41-1.41M17.66 6.34l1.41-1.41" />
        </svg>
      ) : (
        <svg
          viewBox="0 0 24 24"
          className="size-4 scale-110"
          aria-hidden="true"
          fill="none"
          stroke="currentColor"
          strokeWidth="1.75"
        >
          <path d="M21 14.5A8.5 8.5 0 0 1 9.5 3 7 7 0 1 0 21 14.5z" />
        </svg>
      )}
    </button>
  );
}
