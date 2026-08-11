import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { finalize, shareReplay, tap } from 'rxjs/operators';
import { SessionService } from '../auth/session.service';
import { BaseApiService } from '../../shared/services/base-api.service';
import { LoggerService } from './logger.service';
import { normalizeGuid } from '../../shared/utils/guid.utils';

export interface OrganizationDetails {
  tenantId: string;
  organizationName: string;
  organizationCode: string;
  organizationType: string | null;
  logoUrl?: string | null;
  timeZoneId?: string | null;
  languageCode?: string | null;
  hospitals: OrganizationHospital[];
  members: OrganizationMember[];
  createdOn: string;
}

export interface OrganizationHospital {
  id: string;
  name: string;
  code: string;
  address: string | null;
  type: string | null;
  bedCapacity: string | null;
  countryCode: string | null;
  stateCode: string | null;
  isOnboarded: boolean;
  nabhLevel: string | null;
  createdAt: string;
  journey: HospitalJourney[];
}

export interface HospitalJourney {
  /** The hospital's enrollment id. Journey APIs address this. */
  journeyId: string;
  /**
   * The accreditation framework this journey enrolls in. Audit / schedule / compliance APIs filter
   * by this — never by {@link journeyId}. Optional: older Gateway builds omit it.
   */
  frameworkId?: string;
  frameworkCode: string;
  frameworkName: string;
  startDate: string | null;
  expiryDate: string | null;
}

export interface OrganizationMember {
  id: string;
  userId: string;
  /** Canonical role id; use `RoleRegistryService.labelOf` / `roleLabel` pipe in UI. */
  role: string;
  departmentId?: string | null;
  departmentName?: string | null;
  designation?: string | null;
  reportsToUserId?: string | null;
  firstName: string | null;
  lastName: string | null;
  email: string | null;
  phoneNumber: string | null;
  alternatePhoneNumber: string | null;
  profilePicUrl: string | null;
  preferences: unknown | null;
  createdAt: string;
}

@Injectable({
  providedIn: 'root',
})
export class OrganizationService extends BaseApiService {
  private readonly organizationSubject = new BehaviorSubject<OrganizationDetails | null>(null);
  readonly organization$ = this.organizationSubject.asObservable();
  private inFlightOrganizationRequest$: Observable<OrganizationDetails | null> | null = null;

  constructor(
    private readonly sessionService: SessionService,
    private readonly logger: LoggerService,
  ) {
    super();
  }

  loadOrganizationDetails(forceReload = false): Observable<OrganizationDetails | null> {
    if (!forceReload && this.organizationSubject.value) {
      return of(this.organizationSubject.value);
    }

    if (!forceReload && this.inFlightOrganizationRequest$) {
      return this.inFlightOrganizationRequest$;
    }

    const organizationId = normalizeGuid(this.sessionService.getOrganizationId());
    if (!organizationId) {
      this.logger.warn(
        'Organization details fetch skipped because organization id is missing or empty',
      );
      return of(null);
    }

    this.inFlightOrganizationRequest$ = this.get<OrganizationDetails>(
      `/tenant/${organizationId}/organization-detail`,
    ).pipe(
      tap((organization) => this.organizationSubject.next(organization)),
      finalize(() => {
        this.inFlightOrganizationRequest$ = null;
      }),
      shareReplay({ bufferSize: 1, refCount: true }),
    );

    return this.inFlightOrganizationRequest$;
  }

  setOrganization(organization: OrganizationDetails | null): void {
    this.organizationSubject.next(organization);
  }

  getOrganization(): OrganizationDetails | null {
    return this.organizationSubject.value;
  }

  getHospitals(): OrganizationHospital[] {
    return this.organizationSubject.value?.hospitals ?? [];
  }

  /**
   * Resolve a journey id to the framework it enrolls in.
   *
   * The hospital/framework selector works in journeys, but audit, schedule and compliance APIs
   * filter by framework. This is the single mapping point between the two — see
   * {@link ComplianceContextService} for why they must not be interchanged.
   *
   * Returns `null` when the journey is unknown or the Gateway build predates `frameworkId` on the
   * journey payload; callers then omit the framework filter rather than substituting a journey id.
   */
  getFrameworkIdForJourney(hospitalId: string | null, journeyId: string | null): string | null {
    if (!hospitalId || !journeyId) {
      return null;
    }

    const hospital = this.getHospitals().find((h) => h.id === hospitalId);
    return (hospital?.journey ?? []).find((j) => j.journeyId === journeyId)?.frameworkId ?? null;
  }

  getDefaultHospital(): OrganizationHospital | null {
    const hospitals = this.getHospitals();
    if (hospitals.length === 0) {
      return null;
    }

    return hospitals.find((hospital) => hospital.isOnboarded) ?? hospitals[0];
  }

  hasAnyOnboardedHospital(): boolean {
    return this.getHospitals().some((hospital) => hospital.isOnboarded);
  }

  getHospitalJourneys(hospitalId: string): HospitalJourney[] {
    const hospital = this.getHospitals().find((h) => h.id === hospitalId);
    return hospital?.journey ?? [];
  }

  getMemberByUserId(userId: string): OrganizationMember | null {
    const members = this.organizationSubject.value?.members ?? [];
    return members.find((m) => m.userId === userId) ?? null;
  }

  /**
   * Display name for any organization member id.
   *
   * Several APIs return only a user GUID for owner/assignee fields, which would otherwise be shown
   * to the user verbatim. Resolve through here so one user id renders as one name everywhere.
   * Returns `''` when the id is unknown — callers choose their own placeholder.
   */
  getMemberDisplayName(userId: string | null | undefined): string {
    if (!userId) return '';
    const member = this.getMemberByUserId(userId);
    if (!member) return '';
    return [member.firstName, member.lastName].filter(Boolean).join(' ');
  }

  /** @deprecated Naming implies the current user only — use {@link getMemberDisplayName}. */
  getLoggedInMemberDisplayName(userId: string | null): string {
    return this.getMemberDisplayName(userId);
  }

  clearOrganization(): void {
    this.organizationSubject.next(null);
    this.inFlightOrganizationRequest$ = null;
  }

  invalidateCache(): void {
    this.organizationSubject.next(null);
    this.inFlightOrganizationRequest$ = null;
  }
}
