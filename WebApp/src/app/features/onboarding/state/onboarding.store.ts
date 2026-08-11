import { Injectable, inject, signal } from '@angular/core';
import { Organization } from '../models/organization.model';
import { Hospital } from '../models/hospital.model';
import { AdminUser } from '../models/admin-user.model';
import { OnboardingRequest } from '../models/onboarding-request.model';
import { SessionService } from '../../../core/auth/session.service';
import {
  getSignupOnboardingStorageKey,
  LEGACY_SIGNUP_ONBOARDING_KEY,
} from '../../../core/services/onboarding-storage.keys';

@Injectable({
  providedIn: 'root',
})
export class OnboardingStore {
  private readonly session = inject(SessionService);

  organization = signal<Organization | null>(null);
  hospital = signal<Hospital | null>(null);
  adminUser = signal<AdminUser | null>(null);
  currentStep = signal<number>(1);
  /** Framework `code` values from AI onboarding wizard (or future steps); merged into signup hospital on submit when set. */
  frameworkCodes = signal<string[] | null>(null);

  /** Reload signup draft for the current identity (e.g. after login). */
  reinitializeFromSession(): void {
    this.organization.set(null);
    this.hospital.set(null);
    this.adminUser.set(null);
    this.currentStep.set(1);
    this.frameworkCodes.set(null);
    this.loadFromSessionStorage();
  }

  setOrganization(org: Organization): void {
    this.organization.set(org);
    this.saveToSessionStorage();
  }

  setHospital(hosp: Hospital): void {
    this.hospital.set(hosp);
    this.saveToSessionStorage();
  }

  setAdminUser(user: AdminUser): void {
    this.adminUser.set(user);
    this.saveToSessionStorage();
  }

  setCurrentStep(step: number): void {
    this.currentStep.set(step);
    this.saveToSessionStorage();
  }

  setFrameworkCodes(codes: string[]): void {
    this.frameworkCodes.set(codes.length ? codes : null);
    this.saveToSessionStorage();
  }

  /** Codes collected for onboarding payloads (AI wizard + signup merge). */
  getFrameworkCodes(): string[] | null {
    return this.frameworkCodes();
  }

  buildRequestPayload(): OnboardingRequest {
    const org = this.organization();
    const hosp = this.hospital();
    const user = this.adminUser();

    if (!org || !hosp || !user) {
      throw new Error('Cannot build request payload: missing required data');
    }

    const fromWizard = this.frameworkCodes();
    const hospitalPayload: Hospital = {
      ...hosp,
      frameworkCodes:
        fromWizard && fromWizard.length > 0 ? fromWizard : hosp.frameworkCodes,
    };

    return {
      organization: org,
      hospital: hospitalPayload,
      adminUser: user,
    };
  }

  reset(): void {
    this.organization.set(null);
    this.hospital.set(null);
    this.adminUser.set(null);
    this.currentStep.set(1);
    this.frameworkCodes.set(null);
    this.clearSessionStorage();
  }

  private migrateLegacySignupState(): void {
    try {
      const legacy = sessionStorage.getItem(LEGACY_SIGNUP_ONBOARDING_KEY);
      if (!legacy) return;
      const targetKey = getSignupOnboardingStorageKey(this.session.getUserId());
      if (!sessionStorage.getItem(targetKey)) {
        sessionStorage.setItem(targetKey, legacy);
      }
      sessionStorage.removeItem(LEGACY_SIGNUP_ONBOARDING_KEY);
    } catch {
      // Ignore sessionStorage errors
    }
  }

  private saveToSessionStorage(): void {
    try {
      this.migrateLegacySignupState();
      const state = {
        organization: this.organization(),
        hospital: this.hospital(),
        adminUser: this.adminUser(),
        currentStep: this.currentStep(),
        frameworkCodes: this.frameworkCodes(),
      };
      sessionStorage.setItem(getSignupOnboardingStorageKey(this.session.getUserId()), JSON.stringify(state));
    } catch {
      // Ignore sessionStorage errors (e.g., private browsing mode)
    }
  }

  private clearSessionStorage(): void {
    try {
      sessionStorage.removeItem(getSignupOnboardingStorageKey(this.session.getUserId()));
      sessionStorage.removeItem(LEGACY_SIGNUP_ONBOARDING_KEY);
    } catch {
      // Ignore sessionStorage errors
    }
  }

  loadFromSessionStorage(): void {
    try {
      this.migrateLegacySignupState();
      const stored = sessionStorage.getItem(getSignupOnboardingStorageKey(this.session.getUserId()));
      if (!stored) {
        return;
      }
      const state = JSON.parse(stored);
      if (state.organization) this.organization.set(state.organization);
      if (state.hospital) this.hospital.set(state.hospital);
      if (state.adminUser) this.adminUser.set(state.adminUser);
      if (state.currentStep) this.currentStep.set(state.currentStep);
      if (state.frameworkCodes && Array.isArray(state.frameworkCodes)) {
        this.frameworkCodes.set(state.frameworkCodes);
      }
    } catch {
      // Ignore sessionStorage errors
    }
  }
}
