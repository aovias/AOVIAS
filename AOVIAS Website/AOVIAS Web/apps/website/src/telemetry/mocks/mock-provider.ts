import type { Attributes } from '../core/attributes';
import type {
  AnalyticsProvider,
  ProviderInitOptions,
} from '../core/interfaces';
import type { PageView, TelemetryEvent } from '../core/types';

const MAX_BUFFER = 200;

/**
 * In-memory mock analytics provider — the only active sink in Release 1.
 * Never transmits off-device.
 */
export class MockAnalyticsProvider implements AnalyticsProvider {
  readonly id = 'mock';
  readonly displayName = 'AOVIAS Mock Analytics';
  readonly transmits = false;

  private buffer: TelemetryEvent[] = [];
  private debug = false;

  init(options: ProviderInitOptions): void {
    this.debug = options.debug;
  }

  track(event: TelemetryEvent): void {
    this.buffer.push(event);
    if (this.buffer.length > MAX_BUFFER) {
      this.buffer.splice(0, this.buffer.length - MAX_BUFFER);
    }
    if (this.debug && typeof console !== 'undefined') {
      console.debug('[telemetry:mock]', event.name, event.attributes);
    }
  }

  pageView(_page: PageView, event: TelemetryEvent): void {
    this.track(event);
  }

  identify(_visitorId: string, _traits?: Attributes): void {
    // no-op in R1
  }

  flush(): void {
    // no-op — buffer retained for local inspection
  }

  shutdown(): void {
    this.buffer = [];
  }

  /** Test / debug helper — not for UI use. */
  getBufferedEvents(): readonly TelemetryEvent[] {
    return this.buffer;
  }

  clear(): void {
    this.buffer = [];
  }
}

export function createMockAnalyticsProvider(): MockAnalyticsProvider {
  return new MockAnalyticsProvider();
}
