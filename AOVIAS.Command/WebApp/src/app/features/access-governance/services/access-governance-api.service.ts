import { Injectable } from '@angular/core';

import { Observable } from 'rxjs';

import { BaseApiService } from '../../../shared/services/base-api.service';



export interface RoleListItem { id: string; roleCode: string; roleName: string; }

export interface PermissionItem { id: string; code: string; module: string; feature: string; action: string; description: string; permissionGroupValueId?: string; fieldMask?: string; }

export interface PolicyItem { id: string; policyCode: string; policyName: string; status: string; priority: number; }

export interface RoleAssignmentItem {

  id: string; roleDefinitionId: string; roleCode: string; userId: string;

  organizationId?: string; hospitalId?: string; departmentId?: string; employmentId?: string;

  effectiveFrom?: string; effectiveTo?: string; isRevoked: boolean;

}

export interface DelegationItem { id: string; fromUserId: string; toUserId: string; status: string; effectiveFrom: string; effectiveTo?: string; }

export interface ApprovalAuthorityItem { id: string; userId: string; approvalType: string; approvalLevelValueId: string; }

export interface EvaluationTraceStep { step: string; outcome: string; detail?: string; }

export interface AuthorizationDecision {

  allowed: boolean; reason: string;

  matchedRoles: string[]; matchedPermissions: string[]; matchedPolicies: string[];

  matchedDelegations: string[]; matchedResponsibilities: string[];

  evaluationTrace?: EvaluationTraceStep[];

}

export interface EffectivePermissionItem { code: string; sources: string[]; }

export interface PagedRoles<T> { items: T[]; totalCount: number; page: number; pageSize: number; }



@Injectable({ providedIn: 'root' })

export class AccessGovernanceApiService extends BaseApiService {

  listRoles(page = 1, pageSize = 20): Observable<PagedRoles<RoleListItem>> {

    return this.get<PagedRoles<RoleListItem>>('/roles', { page, pageSize });

  }



  listPermissions(groupId?: string): Observable<PermissionItem[]> {

    return this.get<PermissionItem[]>('/permissions', groupId ? { permissionGroupValueId: groupId } : undefined);

  }



  listPolicies(): Observable<PolicyItem[]> {

    return this.get<PolicyItem[]>('/policies');

  }



  listRoleAssignments(userId?: string): Observable<RoleAssignmentItem[]> {

    return this.get<RoleAssignmentItem[]>('/role-assignments', userId ? { userId } : undefined);

  }



  assignRole(body: Record<string, unknown>): Observable<unknown> {

    return this.post('/role-assignments', body);

  }



  revokeAssignment(assignmentId: string): Observable<unknown> {

    return this.post(`/role-assignments/${assignmentId}/revoke`, {});

  }



  listDelegations(): Observable<DelegationItem[]> {

    return this.get<DelegationItem[]>('/delegations');

  }



  listApprovalAuthorities(): Observable<ApprovalAuthorityItem[]> {

    return this.get<ApprovalAuthorityItem[]>('/approval-authorities');

  }



  evaluate(body: Record<string, unknown>): Observable<AuthorizationDecision> {

    return this.post<AuthorizationDecision>('/authorization/evaluate', body);

  }



  effectivePermissions(userId: string): Observable<EffectivePermissionItem[]> {

    return this.get<EffectivePermissionItem[]>('/authorization/effective-permissions', { userId });

  }



  lookups(category: string): Observable<{ id: string; code: string; name: string }[]> {

    return this.get<{ id: string; code: string; name: string }[]>(`/access-governance/lookups/${category}`);

  }

}


