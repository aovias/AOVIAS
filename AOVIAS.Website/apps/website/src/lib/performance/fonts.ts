import { IBM_Plex_Mono, IBM_Plex_Sans } from 'next/font/google';

/**
 * Centralized next/font configuration.
 * - Self-hosted at build (no render-blocking third-party CSS)
 * - `display: 'swap'` minimizes invisible text
 * - Sans is preloaded; mono is secondary (no preload) to reduce contention
 * - Weights limited to those used by the design system
 */
export const fontSans = IBM_Plex_Sans({
  subsets: ['latin'],
  weight: ['400', '500', '600'],
  variable: '--font-ibm-plex-sans',
  // optional + no preload: fallback paints immediately; webfont applies only if already cached.
  display: 'optional',
  preload: false,
  adjustFontFallback: true,
  fallback: ['system-ui', 'Segoe UI', 'sans-serif'],
});

export const fontMono = IBM_Plex_Mono({
  subsets: ['latin'],
  weight: ['400', '500'],
  variable: '--font-ibm-plex-mono',
  display: 'optional',
  preload: false,
  adjustFontFallback: true,
  fallback: ['ui-monospace', 'SFMono-Regular', 'Menlo', 'monospace'],
});

/** Class names to apply on `<html>` for CSS variable wiring. */
export const fontVariableClassName = `${fontSans.variable} ${fontMono.variable}`;

/** Default body face — apply on `<body>`. */
export const fontBodyClassName = fontSans.className;
