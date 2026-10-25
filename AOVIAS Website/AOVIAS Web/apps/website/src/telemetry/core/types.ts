import type { Attributes } from './attributes';
import type { TelemetryContext } from './context';
import type { EventName } from '../registry/event-names';

/** High-level event kinds for routing / consent gating. */
export type TelemetryEventKind =
  | 'page_view'
  | 'navigation'
  | 'interaction'
  | 'conversion'
  | 'error'
  | 'custom'
  | 'commerce'
  | 'identity';

/**
 * TelemetryEvent — OTel "Event" aligned record emitted by the website SDK.
 */
export type TelemetryEvent<TPayload extends Attributes = Attributes> = {
  id: string;
  name: EventName;
  kind: TelemetryEventKind;
  /** Schema version from the event registry. */
  version: number;
  timestamp: string;
  context: TelemetryContext;
  attributes: TPayload;
};

/**
 * Span — timed operation within a trace (page load, checkout step, etc.).
 * Not exported to third-party analytics unless a future OTel exporter is enabled.
 */
export type Span = {
  name: string;
  traceId: string;
  spanId: string;
  parentSpanId?: string;
  startTime: string;
  endTime?: string;
  attributes: Attributes;
  status: 'unset' | 'ok' | 'error';
};

/** Trace — collection handle for related spans (logical unit of work). */
export type Trace = {
  traceId: string;
  name: string;
  attributes: Attributes;
};

export type PageView = {
  path: string;
  title?: string;
  referrer?: string;
  section?: string;
  attributes?: Attributes;
};

export type ConversionEvent = {
  name: EventName;
  value?: number;
  currency?: string;
  attributes?: Attributes;
};

export type ErrorEvent = {
  message: string;
  name?: string;
  stack?: string;
  fatal?: boolean;
  attributes?: Attributes;
};

export type NavigationEvent = {
  action: 'opened' | 'closed' | 'selected';
  target?: string;
  menu?: string;
  attributes?: Attributes;
};
