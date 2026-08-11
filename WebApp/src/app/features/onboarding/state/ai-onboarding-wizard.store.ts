import { Injectable, inject, signal } from '@angular/core';
import {
  AssessmentAnswer,
  HospitalProfileData,
  OnboardingState,
  TeamMember,
} from '../models/onboarding-state.model';
import { OnboardingStore } from './onboarding.store';
import { normalizeRoleId } from '../../../core/auth/roles/role-definitions';
import { SessionService } from '../../../core/auth/session.service';
import { getAiWizardStorageKey, LEGACY_AI_WIZARD_KEY } from '../../../core/services/onboarding-storage.keys';

const WIZARD_VERSION = 4;

function createDefaultState(): OnboardingState {
  return {
    wizardVersion: WIZARD_VERSION,
    currentStep: 1,
    hospitalProfile: {
      hospitalId: '',
      hospitalType: '',
      bedCapacity: '',
      countryCode: '',
      stateCode: '',
      departmentIds: [],
    },
    selectedFrameworks: [],
    selectedFrameworkDisplayNames: [],
    assessmentAnswers: [],
    invitedUsers: [],
    completed: false,
  };
}

function normalizeLoaded(raw: unknown): OnboardingState {
  if (!raw || typeof raw !== 'object') {
    return createDefaultState();
  }
  const o = raw as Record<string, unknown>;
  const base = createDefaultState();
  const loadedVersion = typeof o['wizardVersion'] === 'number' ? o['wizardVersion'] : 1;
  const merged: OnboardingState = {
    ...base,
    ...(o as Partial<OnboardingState>),
    hospitalProfile: {
      ...base.hospitalProfile,
      ...((o['hospitalProfile'] as HospitalProfileData) ?? {}),
    },
    invitedUsers: normalizeTeamMembers((o['invitedUsers'] as TeamMember[]) ?? []),
  };
  merged.wizardVersion = WIZARD_VERSION;
  if (loadedVersion < WIZARD_VERSION) {
    merged.assessmentAnswers = [];
  }
  return merged;
}

function mapRole(raw: string | undefined): string {
  const r = raw?.trim() ?? '';
  if (!r) return '';
  return normalizeRoleId(r) ?? r;
}

/** Legacy session rows used `department` string; map to `departmentId`. Canonicalize role ids. */
function normalizeTeamMembers(members: TeamMember[]): TeamMember[] {
  return (members ?? []).map((m) => {
    const any = m as TeamMember & { department?: string };
    if (typeof any.departmentId === 'string' && any.departmentId) {
      return {
        name: m.name ?? '',
        email: m.email ?? '',
        role: mapRole(m.role),
        departmentId: any.departmentId,
      };
    }
    if (typeof any.department === 'string' && any.department) {
      return {
        name: m.name ?? '',
        email: m.email ?? '',
        role: mapRole(m.role),
        departmentId: any.department,
      };
    }
    return {
      name: m.name ?? '',
      email: m.email ?? '',
      role: mapRole(m.role),
      departmentId: '',
    };
  });
}

@Injectable({
  providedIn: 'root',
})
export class AiOnboardingWizardStore {
  private readonly signupOnboardingStore = inject(OnboardingStore);
  private readonly session = inject(SessionService);

  private readonly _state = signal<OnboardingState>(this.loadFromStorage());

  /** Read-only wizard state. */
  readonly state = this._state.asReadonly();

  /** Reload draft from `sessionStorage` for the current session user (e.g. after login). */
  reinitializeForCurrentUser(): void {
    this._state.set(this.loadFromStorage());
  }

  private migrateLegacyAiWizard(userId: string): void {
    try {
      const legacy = sessionStorage.getItem(LEGACY_AI_WIZARD_KEY);
      if (!legacy) return;
      const key = getAiWizardStorageKey(userId);
      if (!key) return;
      if (!sessionStorage.getItem(key)) {
        sessionStorage.setItem(key, legacy);
      }
      sessionStorage.removeItem(LEGACY_AI_WIZARD_KEY);
    } catch {
      /* ignore */
    }
  }

  private loadFromStorage(): OnboardingState {
    const uid = this.session.getUserId();
    if (!uid) {
      return createDefaultState();
    }
    this.migrateLegacyAiWizard(uid);
    try {
      const key = getAiWizardStorageKey(uid);
      if (!key) return createDefaultState();
      const raw = sessionStorage.getItem(key);
      if (raw) {
        return normalizeLoaded(JSON.parse(raw));
      }
    } catch {
      /* ignore */
    }
    return createDefaultState();
  }

  private persist(): void {
    const uid = this.session.getUserId();
    if (!uid) return;
    const key = getAiWizardStorageKey(uid);
    if (!key) return;
    try {
      sessionStorage.setItem(key, JSON.stringify(this._state()));
    } catch {
      /* ignore */
    }
  }

  patchState(partial: Partial<OnboardingState>): void {
    this._state.update((s) => ({ ...s, ...partial }));
    this.persist();
  }

  setCurrentStep(step: number): void {
    this._state.update((s) => ({ ...s, currentStep: step }));
    this.persist();
  }

  setHospitalProfile(profile: HospitalProfileData): void {
    this._state.update((s) => ({ ...s, hospitalProfile: { ...profile } }));
    this.persist();
  }

  setFrameworks(codes: string[], displayNames: string[]): void {
    this._state.update((s) => ({
      ...s,
      selectedFrameworks: [...codes],
      selectedFrameworkDisplayNames: [...displayNames],
    }));
    this.signupOnboardingStore.setFrameworkCodes(codes);
    this.persist();
  }

  setAssessmentAnswers(answers: AssessmentAnswer[]): void {
    this._state.update((s) => ({ ...s, assessmentAnswers: [...answers] }));
    this.persist();
  }

  setInvitedUsers(users: TeamMember[]): void {
    this._state.update((s) => ({ ...s, invitedUsers: users.map((u) => ({ ...u })) }));
    this.persist();
  }

  setCompleted(completed: boolean): void {
    this._state.update((s) => ({ ...s, completed }));
    this.persist();
  }

  /** Clears AI wizard draft (e.g. after successful onboard submit). */
  clearWizardSession(): void {
    const uid = this.session.getUserId();
    try {
      if (uid) {
        const key = getAiWizardStorageKey(uid);
        if (key) sessionStorage.removeItem(key);
      }
      sessionStorage.removeItem(LEGACY_AI_WIZARD_KEY);
    } catch {
      /* ignore */
    }
    this._state.set(createDefaultState());
  }
}
