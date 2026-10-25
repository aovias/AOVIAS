import type { LucideIcon } from 'lucide-react';

/**
 * Shared marketing component types — content is always passed via props.
 */

export type MarketingCta = {
  label: string;
  href: string;
  external?: boolean;
};

export type MarketingAlign = 'start' | 'center';

export type MarketingWidth = 'narrow' | 'default' | 'wide';

/** Compact icon + label row under hero CTAs (homepage trust strip). */
export type MarketingHighlight = {
  id: string;
  label: string;
  icon: LucideIcon;
  /** When set, hero renders a 2×2 capability grid instead of the trust strip. */
  description?: string;
};
