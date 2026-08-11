import { Injectable, inject } from '@angular/core';
import { Observable, of } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { BaseApiService } from '../../../shared/services/base-api.service';
import { OnboardingJourneyDto } from '../models/onboarding-journey.model';
import {
  TRIAL_PROVISIONING_STORAGE_KEY,
  TrialProvisioningSnapshot,
  getTrialProvisioningStorageKey,
} from '../models/signup-response.model';
import { SessionService } from '../../../core/auth/session.service';

@Injectable({ providedIn: 'root' })
export class OnboardingJourneyService extends BaseApiService {
  private readonly session = inject(SessionService);
  private cached: OnboardingJourneyDto | null = null;

  getJourney(force = false): Observable<OnboardingJourneyDto> {
    if (this.cached && !force) {
      return of(this.cached);
    }
    return this.get<OnboardingJourneyDto>('/onboarding/journey').pipe(
      tap((j) => {
        this.cached = j;
        this.persistTrialSnapshot(j);
      }),
      catchError(() => of(this.fallbackFromStorage())),
    );
  }

  clearCache(): void {
    this.cached = null;
  }

  private persistTrialSnapshot(j: OnboardingJourneyDto): void {
    const snap: TrialProvisioningSnapshot = {
      licenseKeyId: j.trial?.licenseKeyId,
      licenseStatus: j.trial?.licenseStatus,
      trialExpiresAtUtc: j.trial?.expiresAtUtc,
      maxSeats: j.trial?.maxSeats,
      provisioningStatus: j.trial?.isOperational ? 'Completed' : undefined,
      organizationId: j.organizationId,
    };
    const uid = this.session.getUserId();
    const key = getTrialProvisioningStorageKey(uid);
    try {
      sessionStorage.setItem(key, JSON.stringify(snap));
      sessionStorage.removeItem(TRIAL_PROVISIONING_STORAGE_KEY);
    } catch {
      /* ignore */
    }
  }

  private fallbackFromStorage(): OnboardingJourneyDto {
    const uid = this.session.getUserId();
    let snap: TrialProvisioningSnapshot | null = null;
    try {
      const raw =
        sessionStorage.getItem(getTrialProvisioningStorageKey(uid)) ||
        sessionStorage.getItem(TRIAL_PROVISIONING_STORAGE_KEY);
      if (raw) snap = JSON.parse(raw) as TrialProvisioningSnapshot;
    } catch {
      /* ignore */
    }
    const days = snap?.trialExpiresAtUtc
      ? Math.max(0, Math.ceil((new Date(snap.trialExpiresAtUtc).getTime() - Date.now()) / 86400000))
      : 30;
    return {
      organizationId: snap?.organizationId || '',
      organizationName: 'Your organization',
      isOnboarded: false,
      role: 'ORG_ADMIN',
      emailVerified: false,
      progressPercent: 10,
      nextAction: 'Continue setup',
      estimatedMinutesRemaining: 25,
      stages: [],
      trial: {
        licenseKeyId: snap?.licenseKeyId,
        licenseStatus: snap?.licenseStatus || 'Active',
        daysRemaining: days,
        expiresAtUtc: snap?.trialExpiresAtUtc,
        maxSeats: snap?.maxSeats || 10,
        usedSeats: 1,
        entitlements: [],
        isOperational: true,
      },
      quickActions: [{ code: 'continue', label: 'Continue setup', href: '/onboarding' }],
    };
  }
}
