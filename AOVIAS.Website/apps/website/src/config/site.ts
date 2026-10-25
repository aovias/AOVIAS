/**
 * Core site identity. SEO-specific defaults live in `config/seo.ts`.
 * Prefer `getSiteUrl()` from `@/lib/seo` for environment-aware absolute URLs.
 */
export const siteConfig = {
  name: 'AOVIAS',
  shortName: 'AOVIAS',
  description:
    'AOVIAS strengthens institutions through intelligent, responsible technology — built with engineering discipline and long-term commitment.',
  /** Canonical production origin (fallback when env is unset). */
  url: 'https://aovias.com',
  locale: 'en_US',
  language: 'en',
  /** Public release identity — surfaced in the footer. */
  release: 'Release 1',
  version: 'v1.0',
} as const;

export type SiteConfig = typeof siteConfig;
