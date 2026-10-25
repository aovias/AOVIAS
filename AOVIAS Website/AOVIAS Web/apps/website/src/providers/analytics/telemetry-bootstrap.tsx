'use client';

import { PageViewTracker } from '@/telemetry';

/**
 * Analytics provider slot — page-view instrumentation only.
 * Lives under AppProviders → TelemetryProvider (root).
 */
export function TelemetryBootstrap() {
  return <PageViewTracker />;
}
