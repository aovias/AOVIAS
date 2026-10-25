import { featureFlags } from '@/config/feature-flags';
import { integrationConfig } from '@/config/integration';

import type { AnalyticsProviderId } from '../providers/ids';

/**
 * Telemetry runtime config — Release 1 keeps collectors inactive.
 * `enabled` requires an explicit feature flag; mock remains the only provider.
 */
export type TelemetryConfig = {
  /** Master switch — false in R1 unless explicitly enabled for local debugging. */
  enabled: boolean;
  /** When true, mock provider retains an in-memory ring buffer (dev only). */
  debug: boolean;
  /** Active provider ids. R1.7: mock only. */
  providers: readonly AnalyticsProviderId[];
  /** Integration base URL placeholder for future internal analytics. */
  analyticsBaseUrl: string;
  /** Integration mode from R1.5 config. */
  mode: 'mock' | 'http';
};

export function resolveTelemetryConfig(): TelemetryConfig {
  const analytics = integrationConfig.services.analytics;
  const enabled = featureFlags.analyticsEnabled === true;
  const debug =
    process.env['NODE_ENV'] === 'development' ||
    process.env['NEXT_PUBLIC_TELEMETRY_DEBUG'] === 'true';

  return {
    // R1.7: even when the flag is true, only the mock provider is registered.
    // Live providers remain stubs until Release 2 wiring.
    enabled,
    debug,
    providers: ['mock'],
    analyticsBaseUrl: analytics.baseUrl,
    mode: analytics.mode,
  };
}

export const telemetryConfig = resolveTelemetryConfig();
