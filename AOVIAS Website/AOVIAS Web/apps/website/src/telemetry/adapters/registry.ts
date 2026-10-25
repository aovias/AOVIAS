import type { TelemetryClient } from '../core/interfaces';
import { telemetryConfig } from '../config/telemetry-config';
import { createMockAnalyticsProvider } from '../mocks/mock-provider';
import { futureProviderFactories } from '../providers/stub-provider';

import { createTelemetryClient } from './create-client';

let singleton: TelemetryClient | undefined;

/**
 * Resolve the process-wide telemetry client.
 * R1.7 always composes the mock provider; future stubs are available but not attached.
 */
export function getTelemetryClient(): TelemetryClient {
  if (!singleton) {
    const mock = createMockAnalyticsProvider();
    singleton = createTelemetryClient({
      providers: [mock],
      enabled: telemetryConfig.enabled,
      debug: telemetryConfig.debug,
    });
  }
  return singleton;
}

/** Test helper — reset singleton between tests. */
export function resetTelemetryClient(): void {
  singleton = undefined;
}

/**
 * Release 2 helper — construct a client with named future providers (still stubs).
 * Not used by the public site in R1.7.
 */
export function createFutureReadyClient(
  providerIds: ReadonlyArray<keyof typeof futureProviderFactories>,
): TelemetryClient {
  const mock = createMockAnalyticsProvider();
  const stubs = providerIds.map((id) => futureProviderFactories[id]());
  return createTelemetryClient({
    providers: [mock, ...stubs],
    enabled: false,
    debug: telemetryConfig.debug,
  });
}
