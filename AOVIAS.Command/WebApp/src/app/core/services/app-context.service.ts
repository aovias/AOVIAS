import { Injectable, Injector, inject } from '@angular/core';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { catchError, finalize, map, tap } from 'rxjs/operators';
import { UserContextService } from './user-context.service';
import { ComplianceContextService } from './compliance-context.service';
import { SessionService } from '../auth/session.service';
import { PlatformConfigurationService } from '../configuration/platform-configuration.service';
import { AuthService } from '../auth/auth.service';
import { LoggerService } from './logger.service';
import { normalizeGuid } from '../../shared/utils/guid.utils';

@Injectable({
  providedIn: 'root',
})
export class AppContextService {
  private userContext = inject(UserContextService);
  private complianceContext = inject(ComplianceContextService);
  private sessionService = inject(SessionService);
  private logger = inject(LoggerService);
  private platformConfiguration = inject(PlatformConfigurationService);
  /** Lazy resolve to avoid circular DI with AuthService. */
  private injector = inject(Injector);

  private readonly SELECTED_HOSPITAL_KEY = 'selected_hospital_id';
  /**
   * Holds the **journey** id — the key name was always accurate; the value was simply being used as
   * a framework id everywhere downstream. Retained as-is so existing sessions keep working.
   */
  private readonly SELECTED_JOURNEY_KEY = 'selected_framework_journey_id';
  /** The real framework id. New key: older sessions simply have no value and re-select. */
  private readonly SELECTED_FRAMEWORK_KEY = 'selected_framework_id';

  /** Emits the hospital ID every time a hospital switch completes */
  private hospitalSwitchedSubject = new BehaviorSubject<string | null>(null);
  readonly hospitalSwitched$ = this.hospitalSwitchedSubject.asObservable();

  /** Whether a hospital switch is currently in progress */
  private switchingSubject = new BehaviorSubject<boolean>(false);
  readonly switching$ = this.switchingSubject.asObservable();

  switchHospital(hospitalId: string): void {
    this.applyHospitalSwitch$(hospitalId).subscribe();
  }

  /**
   * Switch hospital, journey and framework context together.
   * Used when the Hospital & Framework selector dialog confirms.
   *
   * `journeyId` and `frameworkId` are distinct — see {@link ComplianceContextService}. The selector
   * works in journeys (that is what a hospital enrolls in), so it supplies both.
   */
  switchContext(hospitalId: string, journeyId: string, frameworkId: string | null): void {
    this.applyHospitalSwitch$(hospitalId, journeyId, frameworkId).subscribe();
  }

  /**
   * Re-mints the access token with the target hospital (when different from JWT), then applies UI context.
   * Gateway tenant isolation requires JWT hospital_id to match X-Hospital-Id / query hospitalId.
   */
  applyHospitalSwitch$(
    hospitalId: string,
    journeyId?: string | null,
    frameworkId?: string | null,
  ): Observable<boolean> {
    const normalized = normalizeGuid(hospitalId);
    if (!normalized) {
      return of(false);
    }

    this.logger.info('Switching hospital context', {
      hospitalId: normalized,
      journeyId,
      frameworkId,
    });
    this.switchingSubject.next(true);

    const jwtHospital = normalizeGuid(this.sessionService.getHospitalId());
    const needsTokenReissue = !!jwtHospital && jwtHospital !== normalized;

    const afterToken$ = needsTokenReissue
      ? this.refreshAccessTokenForHospital$(normalized)
      : of(true);

    return afterToken$.pipe(
      tap((ok) => {
        if (!ok) {
          this.logger.warn('Hospital switch aborted: token reissue failed', {
            hospitalId: normalized,
          });
          return;
        }

        // Prefer JWT hospital after refresh (authoritative); fall back to requested id.
        const effectiveHospital = normalizeGuid(this.sessionService.getHospitalId()) ?? normalized;

        this.complianceContext.setHospitalId(effectiveHospital);
        sessionStorage.setItem(this.SELECTED_HOSPITAL_KEY, effectiveHospital);

        // Configuration is tenant-scoped, so a hospital switch invalidates it exactly as a logout
        // does (DEF-CFG-001). Reset drops the cached catalogs *and* the load-once guard; the
        // reload then fetches under the reissued token, which is what makes the Gateway resolve
        // the new organization. Order matters: the token is already effective here.
        this.platformConfiguration.reset();
        this.platformConfiguration.load();

        if (journeyId !== undefined) {
          this.complianceContext.setJourneyId(journeyId);
          if (journeyId) {
            sessionStorage.setItem(this.SELECTED_JOURNEY_KEY, journeyId);
          } else {
            sessionStorage.removeItem(this.SELECTED_JOURNEY_KEY);
          }
        } else {
          this.loadFrameworkForHospital(effectiveHospital);
          sessionStorage.removeItem(this.SELECTED_JOURNEY_KEY);
          sessionStorage.removeItem(this.SELECTED_FRAMEWORK_KEY);
        }

        if (frameworkId !== undefined) {
          this.complianceContext.setFrameworkId(frameworkId);
          if (frameworkId) {
            sessionStorage.setItem(this.SELECTED_FRAMEWORK_KEY, frameworkId);
          } else {
            sessionStorage.removeItem(this.SELECTED_FRAMEWORK_KEY);
          }
        }

        this.hospitalSwitchedSubject.next(effectiveHospital);
        this.logger.info('Hospital context switched', { hospitalId: effectiveHospital });
      }),
      map((ok) => ok),
      finalize(() => this.switchingSubject.next(false)),
      catchError((err) => {
        this.logger.warn('Hospital switch failed', err);
        return of(false);
      }),
    );
  }

  getPersistedHospitalId(): string | null {
    return sessionStorage.getItem(this.SELECTED_HOSPITAL_KEY);
  }

  getPersistedJourneyId(): string | null {
    return sessionStorage.getItem(this.SELECTED_JOURNEY_KEY);
  }

  getPersistedFrameworkId(): string | null {
    return sessionStorage.getItem(this.SELECTED_FRAMEWORK_KEY);
  }

  clearPersistedSelection(): void {
    sessionStorage.removeItem(this.SELECTED_HOSPITAL_KEY);
    sessionStorage.removeItem(this.SELECTED_JOURNEY_KEY);
    sessionStorage.removeItem(this.SELECTED_FRAMEWORK_KEY);
  }

  loadFrameworkForHospital(_hospitalId: string): void {
    // Clear journey + framework to trigger reselection for the newly chosen hospital.
    this.complianceContext.setJourneyId(null);
    this.complianceContext.setFrameworkId(null);
  }

  /**
   * Hydrate ComplianceContextService from sessionStorage and login session before route components load.
   * Playwright and direct deep-links set hospital/framework in sessionStorage; sidebar restores later.
   *
   * JWT hospital_id is authoritative when it conflicts with a stale sessionStorage selection —
   * otherwise Gateway TenantIsolationMiddleware returns 403 on nearly every API call.
   */
  initializeContext(): void {
    const persistedHospitalId = normalizeGuid(this.getPersistedHospitalId());
    const persistedJourneyId = this.getPersistedJourneyId();
    const persistedFrameworkId = this.getPersistedFrameworkId();
    const sessionHospitalId = normalizeGuid(this.sessionService.getHospitalId());
    const sessionOrgId = this.sessionService.getOrganizationId();

    if (sessionOrgId) {
      this.complianceContext.setOrganizationId(sessionOrgId);
    }

    let hospitalId = sessionHospitalId ?? persistedHospitalId;
    if (sessionHospitalId && persistedHospitalId && sessionHospitalId !== persistedHospitalId) {
      this.logger.warn('Clearing stale selected hospital that diverged from JWT', {
        jwtHospitalId: sessionHospitalId,
        persistedHospitalId,
      });
      hospitalId = sessionHospitalId;
      sessionStorage.setItem(this.SELECTED_HOSPITAL_KEY, sessionHospitalId);
      // Journey/framework belong to the previous hospital — drop them.
      sessionStorage.removeItem(this.SELECTED_JOURNEY_KEY);
      sessionStorage.removeItem(this.SELECTED_FRAMEWORK_KEY);
      this.complianceContext.setJourneyId(null);
      this.complianceContext.setFrameworkId(null);
    } else if (hospitalId) {
      sessionStorage.setItem(this.SELECTED_HOSPITAL_KEY, hospitalId);
    }

    if (hospitalId) {
      this.complianceContext.setHospitalId(hospitalId);
    }

    const hospitalAligned =
      !sessionHospitalId || !persistedHospitalId || sessionHospitalId === persistedHospitalId;

    if (hospitalAligned && persistedJourneyId) {
      this.complianceContext.setJourneyId(persistedJourneyId);
    }

    // Sessions created before framework/journey were separated have no framework id. Leaving it
    // null is correct: framework-filtered calls omit the param and return unfiltered results,
    // rather than filtering on a journey id and silently returning nothing.
    if (hospitalAligned && persistedFrameworkId) {
      this.complianceContext.setFrameworkId(persistedFrameworkId);
    }
  }

  initializeFromSession(): void {
    if (this.sessionService.isSessionValid()) {
      // Roles must be restored before any `roleGuard` runs, otherwise a reload or deep link
      // evaluates against an empty role set and rejects a legitimate user.
      this.userContext.hydrateFromToken();
      this.initializeContext();

      // Tenant configuration, fetched once per session. Idempotent, non-blocking, and degrades to
      // the seed catalogs on failure — consumers read signals and update when it lands.
      this.platformConfiguration.load();
    }
  }

  private refreshAccessTokenForHospital$(hospitalId: string): Observable<boolean> {
    const auth = this.injector.get(AuthService);
    return auth.refreshToken(hospitalId).pipe(
      map(() => true),
      catchError((err) => {
        this.logger.warn('Token refresh for hospital switch failed', err);
        return of(false);
      }),
    );
  }
}
