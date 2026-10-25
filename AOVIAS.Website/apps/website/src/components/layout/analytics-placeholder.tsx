'use client';

import { TelemetryBootstrap } from '@/providers/analytics';

/**
 * @deprecated Prefer `TelemetryBootstrap` from `@/providers/analytics`.
 * Kept for layout export compatibility — no third-party scripts.
 */
export function AnalyticsPlaceholder() {
  return <TelemetryBootstrap />;
}
