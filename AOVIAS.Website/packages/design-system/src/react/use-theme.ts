'use client';

import { useTheme as useNextTheme } from 'next-themes';

export type ResolvedTheme = 'light' | 'dark' | undefined;

export function useTheme() {
  const { theme, setTheme, resolvedTheme, systemTheme, themes } = useNextTheme();

  return {
    theme,
    setTheme,
    resolvedTheme: resolvedTheme as ResolvedTheme,
    systemTheme,
    themes,
    setLight: () => {
      setTheme('light');
    },
    setDark: () => {
      setTheme('dark');
    },
    toggle: () => {
      setTheme(resolvedTheme === 'dark' ? 'light' : 'dark');
    },
  };
}
