import type { Attributes } from './attributes';
import type {
  CampaignInformation,
  SessionInformation,
  TelemetryContext,
} from './context';
import type {
  ConversionEvent,
  ErrorEvent,
  NavigationEvent,
  PageView,
  Span,
  TelemetryEvent,
  Trace,
} from './types';
import type { EventName } from '../registry/event-names';
import type { ConsentState } from '../consent/types';

/**
 * AnalyticsProvider — pluggable sink. Pages never import providers directly.
 */
export interface AnalyticsProvider {
  readonly id: string;
  readonly displayName: string;
  /** Whether this provider may transmit off-device when active. */
  readonly transmits: boolean;
  init?(options: ProviderInitOptions): void | Promise<void>;
  track(event: TelemetryEvent): void | Promise<void>;
  pageView?(page: PageView, event: TelemetryEvent): void | Promise<void>;
  identify?(visitorId: string, traits?: Attributes): void | Promise<void>;
  flush?(): void | Promise<void>;
  shutdown?(): void | Promise<void>;
}

export type ProviderInitOptions = {
  context: TelemetryContext;
  consent: ConsentState;
  debug: boolean;
};

/**
 * TelemetryClient — application-facing SDK (dependency inversion boundary).
 */
export interface TelemetryClient {
  readonly isActive: boolean;
  getContext(): TelemetryContext;
  setConsent(consent: ConsentState): void;
  getConsent(): ConsentState;
  setCampaign(campaign: CampaignInformation): void;
  getSession(): SessionInformation | undefined;
  track(name: EventName, attributes?: Attributes): void;
  trackPageView(page: PageView): void;
  trackConversion(conversion: ConversionEvent): void;
  trackError(error: ErrorEvent): void;
  trackNavigation(navigation: NavigationEvent): void;
  trackCustom(name: EventName, attributes?: Attributes): void;
  startSpan(name: string, attributes?: Attributes): Span;
  endSpan(span: Span, status?: Span['status']): void;
  startTrace(name: string, attributes?: Attributes): Trace;
}

export type TelemetryClientFactory = () => TelemetryClient;
