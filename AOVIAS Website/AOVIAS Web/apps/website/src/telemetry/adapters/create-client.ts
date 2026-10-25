import { mergeAttributes, type Attributes } from '../core/attributes';
import {
  createTelemetryContext,
  type CampaignInformation,
  type SessionInformation,
  type TelemetryContext,
} from '../core/context';
import { createEventId, createSpanId, createTraceId } from '../core/ids';
import type {
  AnalyticsProvider,
  TelemetryClient,
} from '../core/interfaces';
import type {
  ConversionEvent,
  ErrorEvent,
  NavigationEvent,
  PageView,
  Span,
  TelemetryEvent,
  Trace,
} from '../core/types';
import {
  allowsConsent,
  createConsentState,
  type ConsentState,
} from '../consent/types';
import { telemetryConfig } from '../config/telemetry-config';
import { getEventDefinition } from '../registry/catalog';
import type { EventName } from '../registry/event-names';
import { validateEventAttributes } from '../registry/validate';

export type CreateTelemetryClientOptions = {
  providers: readonly AnalyticsProvider[];
  consent?: ConsentState;
  context?: Partial<TelemetryContext>;
  /** When false, all track calls no-op (R1 default via feature flag). */
  enabled?: boolean;
  debug?: boolean;
};

/**
 * Compose a TelemetryClient over one or more AnalyticsProviders.
 * Pages depend on this interface only — never on a provider implementation.
 */
export function createTelemetryClient(
  options: CreateTelemetryClientOptions,
): TelemetryClient {
  const providers = [...options.providers];
  let consent = createConsentState(options.consent);
  let context = createTelemetryContext(options.context);
  let session: SessionInformation | undefined;
  const enabled = options.enabled ?? telemetryConfig.enabled;
  const debug = options.debug ?? telemetryConfig.debug;

  for (const provider of providers) {
    provider.init?.({ context, consent, debug });
  }

  function emit(name: EventName, attributes: Attributes = {}): void {
    if (!enabled) return;

    const definition = getEventDefinition(name);
    if (!allowsConsent(consent, definition.consent)) return;

    const validation = validateEventAttributes(name, attributes);
    if (!validation.ok && debug) {
      console.warn(
        `[telemetry] missing attributes for ${name}:`,
        validation.missing,
      );
    }

    const event: TelemetryEvent = {
      id: createEventId(),
      name,
      kind: definition.kind,
      version: definition.version,
      timestamp: new Date().toISOString(),
      context,
      attributes: mergeAttributes(attributes, {
        'telemetry.enabled': enabled,
        'event.version': definition.version,
      }),
    };

    for (const provider of providers) {
      void provider.track(event);
    }
  }

  return {
    get isActive() {
      return enabled && consent.analytics;
    },

    getContext() {
      return context;
    },

    setConsent(next) {
      consent = createConsentState(next);
    },

    getConsent() {
      return consent;
    },

    setCampaign(campaign: CampaignInformation) {
      context = { ...context, campaign };
    },

    getSession() {
      return session;
    },

    track(name, attributes) {
      emit(name, attributes);
    },

    trackPageView(page: PageView) {
      const attributes = mergeAttributes(page.attributes, {
        'page.path': page.path,
        ...(page.title ? { 'page.title': page.title } : {}),
        ...(page.referrer ? { 'page.referrer': page.referrer } : {}),
        ...(page.section ? { 'page.section': page.section } : {}),
      });

      if (!enabled) return;

      const definition = getEventDefinition('page.viewed');
      if (!allowsConsent(consent, definition.consent)) return;

      const event: TelemetryEvent = {
        id: createEventId(),
        name: 'page.viewed',
        kind: 'page_view',
        version: definition.version,
        timestamp: new Date().toISOString(),
        context,
        attributes,
      };

      for (const provider of providers) {
        if (provider.pageView) {
          void provider.pageView(page, event);
        } else {
          void provider.track(event);
        }
      }

      // Section-specific mirrors (inactive unless enabled + consent)
      if (page.section === 'technology' || page.path.startsWith('/technology')) {
        emit('technology.page_viewed', attributes);
        if (page.path.startsWith('/technology/ai')) {
          emit('technology.ai_page_viewed', attributes);
        }
        if (page.path.startsWith('/technology/platform')) {
          emit('technology.platform_page_viewed', attributes);
        }
      }
      if (page.path.startsWith('/pricing')) {
        emit('pricing.viewed', attributes);
      }
      if (page.path.startsWith('/products/')) {
        const slug = page.path.split('/')[2];
        if (slug) {
          emit('product.viewed', mergeAttributes(attributes, { 'product.slug': slug }));
        }
      }
      if (page.path.startsWith('/company/careers')) {
        emit('career.viewed', attributes);
      }
      if (page.path.startsWith('/resources/docs')) {
        emit('resource.docs_visited', attributes);
        emit('conversion.documentation_visit', attributes);
      }
    },

    trackConversion(conversion: ConversionEvent) {
      emit(
        conversion.name,
        mergeAttributes(conversion.attributes, {
          ...(conversion.value !== undefined
            ? { 'conversion.value': conversion.value }
            : {}),
          ...(conversion.currency
            ? { 'conversion.currency': conversion.currency }
            : {}),
        }),
      );
    },

    trackError(error: ErrorEvent) {
      emit(
        'error.captured',
        mergeAttributes(error.attributes, {
          'error.message': error.message,
          ...(error.name ? { 'error.name': error.name } : {}),
          ...(error.fatal !== undefined ? { 'error.fatal': error.fatal } : {}),
        }),
      );
    },

    trackNavigation(navigation: NavigationEvent) {
      const attrs = mergeAttributes(navigation.attributes, {
        ...(navigation.target ? { 'nav.target': navigation.target } : {}),
        ...(navigation.menu ? { 'nav.menu': navigation.menu } : {}),
      });
      if (navigation.action === 'opened') {
        emit('navigation.opened', attrs);
      } else if (navigation.action === 'closed') {
        emit('navigation.closed', attrs);
      } else {
        emit('navigation.item_selected', attrs);
      }
    },

    trackCustom(name, attributes) {
      emit(name, attributes);
    },

    startSpan(name, attributes = {}) {
      const span: Span = {
        name,
        traceId: context.traceId,
        spanId: createSpanId(),
        parentSpanId: context.spanId,
        startTime: new Date().toISOString(),
        attributes,
        status: 'unset',
      };
      return span;
    },

    endSpan(span, status = 'ok') {
      span.endTime = new Date().toISOString();
      span.status = status;
      if (debug) {
        console.debug('[telemetry:span]', span.name, span.status, span);
      }
    },

    startTrace(name, attributes = {}): Trace {
      const traceId = createTraceId();
      context = {
        ...context,
        traceId,
        spanId: createSpanId(),
      };
      session = session ?? {
        sessionId: context.sessionId ?? `ses_${createTraceId().slice(0, 16)}`,
        startedAt: new Date().toISOString(),
        isNewSession: true,
        ...(context.visitorId ? { visitorId: context.visitorId } : {}),
      };
      return { traceId, name, attributes };
    },
  };
}
