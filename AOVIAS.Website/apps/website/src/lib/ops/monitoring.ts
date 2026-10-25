/**
 * Provider-agnostic monitoring integration points.
 * Release 1: no live exporters. Release 2: wire Sentry / Datadog / OTel / etc.
 */

export type MonitoringProviderId =
  | 'noop'
  | 'console'
  | 'opentelemetry'
  | 'sentry'
  | 'datadog'
  | 'uptime';

export type MonitoringEvent = {
  name: string;
  level: 'debug' | 'info' | 'warn' | 'error';
  attributes?: Readonly<Record<string, string | number | boolean>>;
  timestamp?: string;
};

export interface MonitoringProvider {
  readonly id: MonitoringProviderId;
  readonly transmits: boolean;
  record(event: MonitoringEvent): void;
  recordError?(error: Error, attributes?: MonitoringEvent['attributes']): void;
  flush?(): void | Promise<void>;
}

/** Default no-op — safe for production until an exporter is registered. */
export class NoopMonitoringProvider implements MonitoringProvider {
  readonly id = 'noop' as const;
  readonly transmits = false;

  record(_event: MonitoringEvent): void {
    // intentionally empty
  }
}

export class ConsoleMonitoringProvider implements MonitoringProvider {
  readonly id = 'console' as const;
  readonly transmits = false;

  record(event: MonitoringEvent): void {
    if (process.env['NODE_ENV'] === 'development') {
      console.info('[monitoring]', event.name, event.level, event.attributes);
    }
  }

  recordError(error: Error, attributes?: MonitoringEvent['attributes']): void {
    console.error('[monitoring:error]', error.message, attributes);
  }
}

let activeProvider: MonitoringProvider = new NoopMonitoringProvider();

export function getMonitoringProvider(): MonitoringProvider {
  return activeProvider;
}

export function setMonitoringProvider(provider: MonitoringProvider): void {
  activeProvider = provider;
}

export function recordMonitoringEvent(event: MonitoringEvent): void {
  activeProvider.record({
    ...event,
    timestamp: event.timestamp ?? new Date().toISOString(),
  });
}
