import { HttpContextToken } from '@angular/common/http';

/**
 * When true on an outgoing request, {@link journeyLongReadInterceptor} applies
 * `environment.journeyLongReadTimeoutMs` (if positive) via RxJS `timeout`.
 * Set only on Journey aggregation-heavy GETs from {@link JourneyDataService}.
 */
export const JOURNEY_LONG_READ_CONTEXT = new HttpContextToken<boolean>(() => false);
