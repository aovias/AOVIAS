/**
 * AOVIAS Website Telemetry SDK — provider-agnostic analytics readiness.
 * Pages and components import from here only — never from provider packages.
 */

export type { AttributeValue, Attributes } from './core/attributes';
export { mergeAttributes } from './core/attributes';
export type {
  CampaignInformation,
  SessionInformation,
  TelemetryContext,
} from './core/context';
export {
  childSpanContext,
  createTelemetryContext,
} from './core/context';
export type {
  AnalyticsProvider,
  ProviderInitOptions,
  TelemetryClient,
  TelemetryClientFactory,
} from './core/interfaces';
export type {
  ConversionEvent,
  ErrorEvent,
  NavigationEvent,
  PageView,
  Span,
  TelemetryEvent,
  TelemetryEventKind,
  Trace,
} from './core/types';

export {
  allowsConsent,
  createConsentState,
  DEFAULT_CONSENT_STATE,
  type ConsentCategory,
  type ConsentState,
} from './consent/types';

export {
  resolveTelemetryConfig,
  telemetryConfig,
  type TelemetryConfig,
} from './config/telemetry-config';

export { createTelemetryClient } from './adapters/create-client';
export {
  createFutureReadyClient,
  getTelemetryClient,
  resetTelemetryClient,
} from './adapters/registry';

export { createMockAnalyticsProvider, MockAnalyticsProvider } from './mocks/mock-provider';
export {
  createStubProvider,
  futureProviderFactories,
} from './providers/stub-provider';
export {
  ANALYTICS_PROVIDER_IDS,
  type AnalyticsProviderId,
} from './providers/ids';

export {
  eventRegistry,
  getEventDefinition,
  listConversionEvents,
  listEventDefinitions,
  type EventRegistry,
} from './registry/catalog';
export type {
  ConsentRequirement,
  EventDefinition,
  EventOwner,
} from './registry/definition';
export {
  EVENT_NAMES,
  isEventName,
  type EventName,
} from './registry/event-names';
export { validateEventAttributes } from './registry/validate';

export {
  conversionContracts,
  getConversionContract,
  type ConversionContract,
  type ConversionContractId,
} from './events/conversions';
export {
  assertEventNameConvention,
  EVENT_NAMESPACES,
  type EventNamespace,
} from './events/taxonomy';

export { TelemetryProvider, useTelemetryContext } from './context/telemetry-provider';
export { usePageView } from './hooks/use-page-view';
export { useTelemetry } from './hooks/use-telemetry';
export { useTrackEvent } from './hooks/use-track-event';
export { PageViewTracker } from './instrumentation/page-view-tracker';
export {
  resolveSiteSection,
  type SiteSection,
} from './instrumentation/section';
