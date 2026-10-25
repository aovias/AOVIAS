/** Legacy unscoped keys (pre–user-namespacing). */
export const LEGACY_AI_WIZARD_KEY = 'ai_onboarding_wizard_v1';
export const LEGACY_SIGNUP_ONBOARDING_KEY = 'onboarding_state';

const ANONYMOUS_BUCKET = 'anonymous';

/**
 * AI onboarding wizard — only persisted when authenticated (`/onboarding` is behind auth).
 * Returns `null` when there is no user id (do not write sessionStorage).
 */
export function getAiWizardStorageKey(userId: string | null): string | null {
  const u = userId?.trim();
  return u ? `${LEGACY_AI_WIZARD_KEY}:${u}` : null;
}

/**
 * Signup wizard — uses a dedicated bucket when no `userId` (public signup flow).
 */
export function getSignupOnboardingStorageKey(userId: string | null): string {
  const u = userId?.trim();
  return u
    ? `${LEGACY_SIGNUP_ONBOARDING_KEY}:${u}`
    : `${LEGACY_SIGNUP_ONBOARDING_KEY}:${ANONYMOUS_BUCKET}`;
}
