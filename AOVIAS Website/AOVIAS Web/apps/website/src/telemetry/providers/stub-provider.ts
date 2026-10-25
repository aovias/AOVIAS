import type { AnalyticsProvider } from '../core/interfaces';
import type { TelemetryEvent } from '../core/types';
import type { AnalyticsProviderId } from './ids';

/**
 * Placeholder provider — registered for Release 2 wiring only.
 * Does not load scripts, network, or SDKs.
 */
export function createStubProvider(
  id: Exclude<AnalyticsProviderId, 'mock'>,
  displayName: string,
): AnalyticsProvider {
  return {
    id,
    displayName,
    transmits: true,
    init() {
      // Intentionally empty — R2 will replace with a real adapter.
    },
    track(_event: TelemetryEvent) {
      // Drop events until a real adapter is registered.
    },
  };
}

export const futureProviderFactories = {
  'google-analytics': () =>
    createStubProvider('google-analytics', 'Google Analytics (placeholder)'),
  'microsoft-clarity': () =>
    createStubProvider('microsoft-clarity', 'Microsoft Clarity (placeholder)'),
  posthog: () => createStubProvider('posthog', 'PostHog (placeholder)'),
  mixpanel: () => createStubProvider('mixpanel', 'Mixpanel (placeholder)'),
  plausible: () => createStubProvider('plausible', 'Plausible (placeholder)'),
  opentelemetry: () =>
    createStubProvider('opentelemetry', 'OpenTelemetry Exporter (placeholder)'),
  internal: () =>
    createStubProvider('internal', 'AOVIAS Internal Analytics (placeholder)'),
} as const;
