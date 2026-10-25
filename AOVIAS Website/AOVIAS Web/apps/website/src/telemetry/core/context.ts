import type { Attributes } from './attributes';
import { createSpanId, createTraceId } from './ids';

/**
 * Telemetry context — mirrors OpenTelemetry Context / baggage concepts
 * without requiring the OTel SDK at runtime.
 */
export type TelemetryContext = {
  traceId: string;
  spanId: string;
  parentSpanId?: string;
  /** Logical session (anonymous until identity). */
  sessionId?: string;
  /** Stable anonymous visitor id when available. */
  visitorId?: string;
  /** Locale / language. */
  locale?: string;
  /** Campaign / UTM bag. */
  campaign?: CampaignInformation;
  /** Arbitrary baggage attributes. */
  baggage?: Attributes;
};

export type CampaignInformation = {
  source?: string;
  medium?: string;
  campaign?: string;
  term?: string;
  content?: string;
};

export type SessionInformation = {
  sessionId: string;
  startedAt: string;
  visitorId?: string;
  isNewSession: boolean;
};

export function createTelemetryContext(
  partial: Partial<TelemetryContext> = {},
): TelemetryContext {
  return {
    traceId: partial.traceId ?? createTraceId(),
    spanId: partial.spanId ?? createSpanId(),
    ...(partial.parentSpanId ? { parentSpanId: partial.parentSpanId } : {}),
    ...(partial.sessionId ? { sessionId: partial.sessionId } : {}),
    ...(partial.visitorId ? { visitorId: partial.visitorId } : {}),
    ...(partial.locale ? { locale: partial.locale } : {}),
    ...(partial.campaign ? { campaign: partial.campaign } : {}),
    ...(partial.baggage ? { baggage: partial.baggage } : {}),
  };
}

export function childSpanContext(parent: TelemetryContext): TelemetryContext {
  return {
    ...parent,
    parentSpanId: parent.spanId,
    spanId: createSpanId(),
  };
}
