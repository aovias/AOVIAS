import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../environments/environment';

const base = () => `${environment.apiBaseUrl}/commercial-admin`;

function unwrap<T>(body: any): T {
  return (body?.data ?? body) as T;
}

@Injectable({ providedIn: 'root' })
export class CommercialAdminApiService {
  private readonly http = inject(HttpClient);

  mePermissions(): Observable<{ roles: string[]; permissions: string[] }> {
    // Passing `unwrap` by reference leaves T uninferrable, so it resolves to
    // `unknown` and fails to match the declared return type. The explicit type
    // argument is what ties the two together.
    return this.http
      .get<any>(`${base()}/me/permissions`)
      .pipe(map((body) => unwrap<{ roles: string[]; permissions: string[] }>(body)));
  }

  health(): Observable<any> {
    return this.http.get(`${base()}/health`).pipe(map(unwrap));
  }

  lifecycleHealth(): Observable<any> {
    return this.http.get(`${base()}/trials/lifecycle/health`).pipe(map(unwrap));
  }

  runLifecycle(reason = 'Manual evaluation from console'): Observable<any> {
    return this.http
      .post(`${base()}/trials/lifecycle/run`, { reason, confirmed: true })
      .pipe(map(unwrap));
  }

  searchOrgs(q: string, status?: string, page = 1): Observable<any> {
    const params: Record<string, string> = { q, page: String(page), pageSize: '25' };
    if (status) params['status'] = status;
    return this.http.get(`${base()}/organizations`, { params }).pipe(map(unwrap));
  }

  getOrg(id: string): Observable<any> {
    return this.http.get(`${base()}/organizations/${id}`).pipe(map(unwrap));
  }

  setOrgStatus(id: string, status: string, reason: string): Observable<any> {
    return this.http
      .post(`${base()}/organizations/${id}/status`, { status, reason, confirmed: true })
      .pipe(map(unwrap));
  }

  searchUsers(q: string, page = 1): Observable<any> {
    return this.http
      .get(`${base()}/users`, { params: { q, page: String(page), pageSize: '25' } })
      .pipe(map(unwrap));
  }

  userAction(userId: string, action: string, reason: string): Observable<any> {
    return this.http
      .post(`${base()}/users/${userId}/${action}`, { reason, confirmed: true })
      .pipe(map(unwrap));
  }

  searchTrials(q: string, page = 1): Observable<any> {
    return this.http
      .get(`${base()}/trials`, { params: { q, page: String(page), pageSize: '25' } })
      .pipe(map(unwrap));
  }

  extendTrial(orgId: string, days: number, reason: string): Observable<any> {
    return this.http
      .post(`${base()}/organizations/${orgId}/trials/extend`, { days, reason, confirmed: true })
      .pipe(map(unwrap));
  }

  expireTrial(orgId: string, reason: string): Observable<any> {
    return this.http
      .post(`${base()}/organizations/${orgId}/trials/expire`, { reason, confirmed: true })
      .pipe(map(unwrap));
  }

  convertTrial(orgId: string, reason: string): Observable<any> {
    return this.http
      .post(`${base()}/organizations/${orgId}/trials/convert`, {
        reason,
        confirmed: true,
        validDays: 365,
      })
      .pipe(map(unwrap));
  }

  searchLicenses(q: string, page = 1): Observable<any> {
    return this.http
      .get(`${base()}/licenses`, { params: { q, page: String(page), pageSize: '25' } })
      .pipe(map(unwrap));
  }

  revokeLicense(licenseKeyId: string, reason: string): Observable<any> {
    return this.http
      .post(`${base()}/licenses/${licenseKeyId}/revoke`, { reason, confirmed: true })
      .pipe(map(unwrap));
  }

  licensingSummary(organizationId: string): Observable<any> {
    return this.http
      .get(`${environment.apiBaseUrl}/licensing/summary`, {
        params: { organizationId },
      })
      .pipe(map(unwrap));
  }

  catalogBackfill(): Observable<any> {
    return this.http.post(`${environment.apiBaseUrl}/licenses/catalog/backfill`, {}).pipe(map(unwrap));
  }

  sendEmail(payload: Record<string, unknown>): Observable<any> {
    return this.http.post(`${base()}/email/send`, payload).pipe(map(unwrap));
  }

  emailLogs(orgId?: string): Observable<any> {
    const params: Record<string, string> = { page: '1', pageSize: '50' };
    if (orgId) params['organizationId'] = orgId;
    return this.http.get(`${base()}/email/logs`, { params }).pipe(map(unwrap));
  }

  previewEmail(template: string): Observable<any> {
    return this.http
      .get(`${base()}/email/preview`, { params: { template } })
      .pipe(map(unwrap));
  }

  support(orgId: string): Observable<any> {
    return this.http.get(`${base()}/support/${orgId}`).pipe(map(unwrap));
  }

  audit(orgId?: string): Observable<any> {
    const params: Record<string, string> = { page: '1', pageSize: '50' };
    if (orgId) params['organizationId'] = orgId;
    return this.http.get(`${base()}/audit`, { params }).pipe(map(unwrap));
  }

  globalSearch(q: string): Observable<any> {
    return this.http.get(`${base()}/search`, { params: { q } }).pipe(map(unwrap));
  }
}
