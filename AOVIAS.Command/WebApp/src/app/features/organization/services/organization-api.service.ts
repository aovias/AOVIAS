import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { BaseApiService } from '../../../shared/services/base-api.service';

export interface MembershipItem {
  id: string;
  organizationId: string;
  userId: string;
  personId?: string;
  role: string;
  membershipStatus: string;
  isPrimary: boolean;
  effectiveFrom: string;
  hospitalId?: string;
  departmentId?: string;
  departmentName?: string;
  designation?: string;
  firstName?: string;
  lastName?: string;
  email?: string;
  createdAt?: string;
}

export interface OrganizationTreeNode {
  id: string;
  name: string;
  nodeType: string;
  children?: OrganizationTreeNode[];
}

export interface DepartmentTreeNode {
  id: string;
  code: string;
  name: string;
  isActive?: boolean;
  hospitalId?: string;
  parentDepartmentId?: string;
  children?: DepartmentTreeNode[];
}

export interface OrganizationalUnitNode {
  id: string;
  organizationId: string;
  hospitalId?: string;
  code: string;
  name: string;
  parentUnitId?: string;
  unitTypeValueId: string;
  unitTypeCode: string;
  unitTypeName: string;
  status: string;
  isActive?: boolean;
  children?: OrganizationalUnitNode[];
}

export interface OrganizationalUnitType {
  id: string;
  code: string;
  name: string;
  sortOrder: number;
}

export interface GraphDto {
  nodes: { id: string; label: string; nodeType: string }[];
  edges: { from: string; to: string; relation: string }[];
}

export interface PagedMemberships<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface HospitalSummary {
  id: string;
  name: string;
  code?: string;
  organizationId?: string;
  parentOrganizationalUnitId?: string;
  isActive?: boolean;
}

@Injectable({ providedIn: 'root' })
export class OrganizationApiService extends BaseApiService {
  private unwrap<T>(r: T | { data: T }): T {
    return r && typeof r === 'object' && 'data' in (r as object)
      ? (r as { data: T }).data
      : (r as T);
  }

  listMemberships(
    organizationId?: string,
    page = 1,
    pageSize = 20,
    options?: {
      search?: string;
      role?: string;
      departmentId?: string;
      membershipStatus?: string;
      includeInactive?: boolean;
    },
  ): Observable<PagedMemberships<MembershipItem>> {
    return this.get<PagedMemberships<MembershipItem> | { data: PagedMemberships<MembershipItem> }>('/membership', {
      organizationId,
      page,
      pageSize,
      search: options?.search,
      role: options?.role,
      departmentId: options?.departmentId,
      membershipStatus: options?.membershipStatus,
      includeInactive: options?.includeInactive,
    }).pipe(map((r) => this.unwrap(r)));
  }

  getOrganizationTree(organizationId: string): Observable<OrganizationTreeNode> {
    return this.get<OrganizationTreeNode | { data: OrganizationTreeNode }>('/organizations/tree', { organizationId }).pipe(
      map((r) => this.unwrap(r)),
    );
  }

  getDepartmentTree(organizationId: string, hospitalId?: string): Observable<DepartmentTreeNode[]> {
    return this.get<DepartmentTreeNode[] | { data: DepartmentTreeNode[] }>('/departments/tree', {
      organizationId,
      hospitalId,
    }).pipe(map((r) => this.unwrap(r)));
  }

  createDepartment(body: {
    organizationId: string;
    hospitalId?: string;
    parentDepartmentId?: string;
    code: string;
    name: string;
  }): Observable<DepartmentTreeNode> {
    return this.post<DepartmentTreeNode | { data: DepartmentTreeNode }>('/departments', body).pipe(map((r) => this.unwrap(r)));
  }

  getOrganizationalUnitTree(
    organizationId: string,
    hospitalId?: string,
    includeInactive = false,
  ): Observable<OrganizationalUnitNode[]> {
    return this.get<OrganizationalUnitNode[] | { data: OrganizationalUnitNode[] }>('/organizational-units/tree', {
      organizationId,
      hospitalId,
      includeInactive,
    }).pipe(map((r) => this.unwrap(r)));
  }

  getOrganizationalUnitTypes(): Observable<OrganizationalUnitType[]> {
    return this.get<OrganizationalUnitType[] | { data: OrganizationalUnitType[] }>('/organizational-units/types').pipe(
      map((r) => this.unwrap(r)),
    );
  }

  createOrganizationalUnit(body: {
    organizationId: string;
    hospitalId?: string;
    parentUnitId?: string;
    unitTypeValueId: string;
    code: string;
    name: string;
  }): Observable<OrganizationalUnitNode> {
    return this.post<OrganizationalUnitNode | { data: OrganizationalUnitNode }>('/organizational-units', body).pipe(
      map((r) => this.unwrap(r)),
    );
  }

  updateOrganizationalUnit(
    id: string,
    body: { code?: string; name?: string; hospitalId?: string; unitTypeValueId?: string },
  ): Observable<OrganizationalUnitNode> {
    return this.put<OrganizationalUnitNode | { data: OrganizationalUnitNode }>(`/organizational-units/${id}`, body).pipe(
      map((r) => this.unwrap(r)),
    );
  }

  moveOrganizationalUnit(id: string, newParentUnitId?: string | null): Observable<OrganizationalUnitNode> {
    return this.post<OrganizationalUnitNode | { data: OrganizationalUnitNode }>(`/organizational-units/${id}/move`, {
      newParentUnitId: newParentUnitId ?? null,
    }).pipe(map((r) => this.unwrap(r)));
  }

  activateOrganizationalUnit(id: string): Observable<OrganizationalUnitNode> {
    return this.post<OrganizationalUnitNode | { data: OrganizationalUnitNode }>(
      `/organizational-units/${id}/activate`,
      {},
    ).pipe(map((r) => this.unwrap(r)));
  }

  deactivateOrganizationalUnit(id: string): Observable<OrganizationalUnitNode> {
    return this.post<OrganizationalUnitNode | { data: OrganizationalUnitNode }>(
      `/organizational-units/${id}/deactivate`,
      {},
    ).pipe(map((r) => this.unwrap(r)));
  }

  deleteOrganizationalUnit(id: string): Observable<unknown> {
    return this.delete(`/organizational-units/${id}`);
  }

  getOrganizationGraph(organizationId: string): Observable<GraphDto> {
    return this.get<GraphDto | { data: GraphDto }>('/organization/graph', { organizationId }).pipe(map((r) => this.unwrap(r)));
  }

  getMembershipHistory(
    membershipId: string,
  ): Observable<{ id: string; fromStatus: string; toStatus: string; recordedAt: string }[]> {
    return this.get('/membership/history', { membershipId });
  }

  listHospitals(organizationId: string): Observable<HospitalSummary[]> {
    return this.get<HospitalSummary[] | { data: HospitalSummary[] } | { items: HospitalSummary[] }>(
      '/hospitals',
      { organizationId },
    ).pipe(
      map((r) => {
        const unwrapped = this.unwrap(r as HospitalSummary[] | { data: HospitalSummary[] });
        if (Array.isArray(unwrapped)) return unwrapped;
        if (unwrapped && typeof unwrapped === 'object' && 'items' in unwrapped) {
          return (unwrapped as { items: HospitalSummary[] }).items;
        }
        return [];
      }),
    );
  }
}
