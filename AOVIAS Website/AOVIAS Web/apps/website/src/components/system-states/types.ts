import type { ReactNode } from 'react';

/**
 * Shared action for system states — link and/or button handler.
 * Presentation only; no retry/API logic lives here.
 */
export type StateAction = {
  label: string;
  href?: string;
  external?: boolean;
  /** Client handler when rendering as a button (e.g. retry). */
  onClick?: () => void;
};

export type StateIllustration = {
  /** Accessible label for the placeholder region. */
  label?: string;
  /** Custom illustration node; omit for dashed placeholder. */
  node?: ReactNode;
};

export type EmptyStateVariant = 'default' | 'subtle' | 'plain';
export type ErrorStateVariant = 'generic' | 'not-found' | 'unavailable' | 'permission';
export type LoadingStateSize = 'sm' | 'md' | 'lg';
export type NoResultsVariant = 'generic' | 'filtered' | 'category';
