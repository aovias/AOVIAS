import { Injectable, inject } from '@angular/core';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { AccessGovernanceApiService } from '../../features/access-governance/services/access-governance-api.service';
import { SessionService } from '../../core/auth/session.service';

@Injectable({ providedIn: 'root' })
export class AuthorizationService {
  private api = inject(AccessGovernanceApiService);
  private session = inject(SessionService);

  can(permissionCode: string): Observable<boolean> {
    const userId = this.session.getUserId();
    if (!userId) return of(false);
    const [resource, ...rest] = permissionCode.split('.');
    const action = rest.join('.') || permissionCode;
    const organizationId = this.session.getOrganizationId();
    const hospitalId = this.session.getHospitalId();
    return this.api
      .evaluate({
        subjectId: userId,
        resource,
        action,
        ...(organizationId ? { organizationId } : {}),
        ...(hospitalId ? { hospitalId } : {}),
      })
      .pipe(
      map((d) => d.allowed),
      catchError(() => of(false)),
    );
  }
}
