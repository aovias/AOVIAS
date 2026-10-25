'use client';

import { ThemeProvider as NextThemesProvider } from 'next-themes';
import type { ReactNode } from 'react';

export type ThemeProviderProps = {
  children: ReactNode;
  /** Storage key for persistence. */
  storageKey?: string;
  /** Default theme before user preference is known. */
  defaultTheme?: 'light' | 'dark' | 'system';
};

/**
 * Shared AOVIAS theme provider.
 * Persists selection via localStorage and sets data-theme on <html>.
 */
export function ThemeProvider({
  children,
  storageKey = 'aovias-theme',
  defaultTheme = 'dark',
}: ThemeProviderProps) {
  return (
    <NextThemesProvider
      attribute="data-theme"
      defaultTheme={defaultTheme}
      enableSystem
      disableTransitionOnChange
      storageKey={storageKey}
      themes={['light', 'dark']}
    >
      {children}
    </NextThemesProvider>
  );
}
