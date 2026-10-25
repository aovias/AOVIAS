import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { normalizeGuid } from '../../shared/utils/guid.utils';

@Injectable({
  providedIn: 'root',
})
/**
 * Ambient compliance context: which organization, hospital, framework and journey the user is
 * working in.
 *
 * **Framework vs journey.** These are two different entities and must never be interchanged:
 * - `frameworkId` — the accreditation framework (e.g. NABH Full Accreditation). Audit, schedule and
 *   compliance APIs filter by this.
 * - `journeyId` — a *hospital's enrollment* in that framework. Journey APIs address this.
 *
 * The two were previously conflated: the selector only had a journey id, stored it as
 * `frameworkId`, and sent it to endpoints that filter by framework — which silently matched
 * nothing. `organization-detail` now returns both, so they are tracked separately here.
 */
@Injectable({
  providedIn: 'root',
})
export class ComplianceContextService {
  private hospitalIdSubject = new BehaviorSubject<string | null>(null);
  private frameworkIdSubject = new BehaviorSubject<string | null>(null);
  private journeyIdSubject = new BehaviorSubject<string | null>(null);
  private organizationIdSubject = new BehaviorSubject<string | null>(null);

  public hospitalId$ = this.hospitalIdSubject.asObservable();
  public frameworkId$ = this.frameworkIdSubject.asObservable();
  public journeyId$ = this.journeyIdSubject.asObservable();
  public organizationId$ = this.organizationIdSubject.asObservable();

  setHospitalId(hospitalId: string | null): void {
    this.hospitalIdSubject.next(normalizeGuid(hospitalId));
  }

  /** The accreditation framework id. Not the journey id — see the class doc. */
  setFrameworkId(frameworkId: string | null): void {
    this.frameworkIdSubject.next(normalizeGuid(frameworkId));
  }

  /** The hospital's enrollment (journey) id. Not the framework id — see the class doc. */
  setJourneyId(journeyId: string | null): void {
    this.journeyIdSubject.next(normalizeGuid(journeyId));
  }

  setOrganizationId(organizationId: string | null): void {
    this.organizationIdSubject.next(normalizeGuid(organizationId));
  }

  getHospitalId(): string | null {
    return this.hospitalIdSubject.value;
  }

  getFrameworkId(): string | null {
    return this.frameworkIdSubject.value;
  }

  getJourneyId(): string | null {
    return this.journeyIdSubject.value;
  }

  getOrganizationId(): string | null {
    return this.organizationIdSubject.value;
  }

  clear(): void {
    this.hospitalIdSubject.next(null);
    this.frameworkIdSubject.next(null);
    this.journeyIdSubject.next(null);
    this.organizationIdSubject.next(null);
  }
}
