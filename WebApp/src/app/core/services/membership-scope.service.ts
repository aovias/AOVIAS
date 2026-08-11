import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { SessionService } from '../auth/session.service';
import { normalizeRoleId } from '../auth/roles/role-definitions';
import { UserContextService } from './user-context.service';
import {
  OrganizationDetails,
  OrganizationMember,
  OrganizationService,
} from './organization.service';

const DEPT_HEAD_DESIGNATION =
  /department\s*head|head\s*-|superintendent|in[- ]charge|accreditation head/i;

/**
 * Resolves the logged-in member's department scope and Department Head designation
 * for dept-scoped filters and persona routing.
 */
@Injectable({ providedIn: 'root' })
export class MembershipScopeService {
  private readonly org = inject(OrganizationService);
  private readonly session = inject(SessionService);
  private readonly userContext = inject(UserContextService);

  ensureLoaded(forceReload = false): Observable<OrganizationDetails | null> {
    return this.org.loadOrganizationDetails(forceReload);
  }

  getCurrentMember(): OrganizationMember | null {
    const userId = this.session.getUserId();
    if (!userId) return null;
    return this.org.getMemberByUserId(userId);
  }

  /** Department GUID from organization membership, when present. */
  getDepartmentId(): string | null {
    const id = this.getCurrentMember()?.departmentId?.trim();
    return id || null;
  }

  getDepartmentName(): string | null {
    const name = this.getCurrentMember()?.departmentName?.trim();
    return name || null;
  }

  hasDepartmentHeadRole(roles?: string[]): boolean {
    const raw = roles ?? this.userContext.getUserRoles();
    return raw.some((r) => {
      const n = normalizeRoleId(r) ?? r;
      return (
        n === 'DEPARTMENT_HEAD' ||
        r === 'DEPARTMENT_HEAD' ||
        r === 'DepartmentHead' ||
        /department.?head/i.test(r)
      );
    });
  }

  hasDepartmentHeadDesignation(member?: OrganizationMember | null): boolean {
    const m = member ?? this.getCurrentMember();
    const designation = m?.designation?.trim() ?? '';
    return !!designation && DEPT_HEAD_DESIGNATION.test(designation);
  }

  /** True when JWT/roles or membership designation indicate Department Head (not QM). */
  isDepartmentHeadExperience(roles?: string[]): boolean {
    if (this.hasDepartmentHeadRole(roles)) return true;
    return this.hasDepartmentHeadDesignation();
  }

  /** Ensures org is loaded then returns departmentId (or null). */
  resolveDepartmentId$(): Observable<string | null> {
    return this.ensureLoaded().pipe(map(() => this.getDepartmentId()));
  }

  resolveDepartmentScope$(): Observable<{
    departmentId: string | null;
    departmentName: string | null;
    isDepartmentHead: boolean;
  }> {
    return this.ensureLoaded().pipe(
      map(() => ({
        departmentId: this.getDepartmentId(),
        departmentName: this.getDepartmentName(),
        isDepartmentHead: this.isDepartmentHeadExperience(),
      })),
    );
  }

  /** No-op helper when caller already has org cached. */
  peekScope(): {
    departmentId: string | null;
    departmentName: string | null;
    isDepartmentHead: boolean;
  } {
    if (!this.org.getOrganization()) {
      return {
        departmentId: null,
        departmentName: null,
        isDepartmentHead: this.hasDepartmentHeadRole(),
      };
    }
    return {
      departmentId: this.getDepartmentId(),
      departmentName: this.getDepartmentName(),
      isDepartmentHead: this.isDepartmentHeadExperience(),
    };
  }
}
