'use client';

import { usePageView } from '../hooks/use-page-view';

/**
 * Mount once under TelemetryProvider — instruments every route via pathname.
 * Does not call third-party providers; events stay in the mock sink when active.
 */
export function PageViewTracker(): null {
  usePageView();
  return null;
}
