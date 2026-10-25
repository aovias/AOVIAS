/**
 * Consent readiness — injectable state for Release 2 consent UI.
 * No consent UI is implemented in R1.7.
 */

export type ConsentCategory =
  | 'functional'
  | 'analytics'
  | 'marketing'
  | 'preferences';

export type ConsentState = Readonly<Record<ConsentCategory, boolean>>;

export const DEFAULT_CONSENT_STATE: ConsentState = {
  functional: true,
  analytics: false,
  marketing: false,
  preferences: false,
};

export function createConsentState(
  partial: Partial<ConsentState> = {},
): ConsentState {
  return {
    functional: partial.functional ?? DEFAULT_CONSENT_STATE.functional,
    analytics: partial.analytics ?? DEFAULT_CONSENT_STATE.analytics,
    marketing: partial.marketing ?? DEFAULT_CONSENT_STATE.marketing,
    preferences: partial.preferences ?? DEFAULT_CONSENT_STATE.preferences,
  };
}

export function allowsConsent(
  state: ConsentState,
  requirement: 'none' | 'analytics' | 'marketing',
): boolean {
  if (requirement === 'none') return true;
  if (requirement === 'analytics') return state.analytics;
  return state.marketing;
}
