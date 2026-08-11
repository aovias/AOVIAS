import { Injectable, inject } from '@angular/core';
import { SessionService } from '../auth/session.service';
import { AiOnboardingWizardStore } from '../../features/onboarding/state/ai-onboarding-wizard.store';
import { OnboardingStore } from '../../features/onboarding/state/onboarding.store';
import {
  getAiWizardStorageKey,
  getSignupOnboardingStorageKey,
  LEGACY_AI_WIZARD_KEY,
  LEGACY_SIGNUP_ONBOARDING_KEY,
} from './onboarding-storage.keys';
import {
  TRIAL_PROVISIONING_STORAGE_KEY,
  getTrialProvisioningStorageKey,
} from '../../features/onboarding/models/signup-response.model';

/**
 * Coordinates onboarding draft keys in `sessionStorage` per user and cleanup on logout / user switch.
 */
@Injectable({
  providedIn: 'root',
})
export class OnboardingStateIsolationService {
  private readonly session = inject(SessionService);
  private readonly aiWizard = inject(AiOnboardingWizardStore);
  private readonly signupOnboarding = inject(OnboardingStore);

  /** Removes legacy unscoped keys (one-time hygiene). */
  clearLegacyOnboardingSessionStorage(): void {
    try {
      sessionStorage.removeItem(LEGACY_AI_WIZARD_KEY);
      sessionStorage.removeItem(LEGACY_SIGNUP_ONBOARDING_KEY);
      sessionStorage.removeItem(TRIAL_PROVISIONING_STORAGE_KEY);
    } catch {
      /* ignore */
    }
  }

  /** Removes namespaced draft keys for a specific user id. */
  purgeStorageForUser(userId: string): void {
    const u = userId?.trim();
    if (!u) return;
    try {
      const k1 = getAiWizardStorageKey(u);
      const k2 = getSignupOnboardingStorageKey(u);
      if (k1) sessionStorage.removeItem(k1);
      sessionStorage.removeItem(k2);
      sessionStorage.removeItem(getTrialProvisioningStorageKey(u));
    } catch {
      /* ignore */
    }
  }

  /** Public signup bucket — clear when a real user logs in so drafts do not bleed. */
  purgeAnonymousSignupStorage(): void {
    try {
      sessionStorage.removeItem(getSignupOnboardingStorageKey(null));
    } catch {
      /* ignore */
    }
  }

  /**
   * Reset in-memory onboarding stores and remove their storage keys for the **current** session user.
   * Call **before** `SessionService.clearSession()` so `getUserId()` is still available.
   */
  resetInMemoryStores(): void {
    this.aiWizard.clearWizardSession();
    this.signupOnboarding.reset();
  }

  /**
   * Full logout: legacy keys, current user namespaced keys, anonymous signup, then store reset.
   * Invoke **before** `SessionService.clearSession()` so storage keys match the current user.
   */
  logoutCleanup(): void {
    const uid = this.session.getUserId();
    this.clearLegacyOnboardingSessionStorage();
    if (uid) {
      this.purgeStorageForUser(uid);
    }
    this.purgeAnonymousSignupStorage();
    this.aiWizard.clearWizardSession();
    this.signupOnboarding.reset();
  }

  /**
   * After login or when identity changes: drop previous user's drafts, clear legacy, drop anonymous signup, reload stores for the new id.
   */
  handleLoginIdentity(priorUserId: string | null, newUserId: string): void {
    if (priorUserId && priorUserId !== newUserId) {
      this.purgeStorageForUser(priorUserId);
    }
    this.clearLegacyOnboardingSessionStorage();
    this.purgeAnonymousSignupStorage();
    this.aiWizard.reinitializeForCurrentUser();
    this.signupOnboarding.reinitializeFromSession();
  }
}
