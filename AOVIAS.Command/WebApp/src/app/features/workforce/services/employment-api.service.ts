import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from '../../../shared/services/base-api.service';

export interface EmploymentListItem {
  id: string;
  employmentNumber: string;
  personSearchName: string;
  personId: string;
  organizationId: string;
  hospitalId: string;
  statusName?: string;
  primaryPositionName?: string;
  joiningDate?: string;
}

export interface EmploymentDetail extends EmploymentListItem {
  userId?: string;
  employmentTypeValueId?: string;
  employmentStatusValueId?: string;
  confirmationDate?: string;
  probationEndDate?: string;
  separationDate?: string;
  noticePeriodDays?: number;
  positionAssignments: PositionAssignment[];
  departmentAssignments: DepartmentAssignment[];
  reportingRelationships: ReportingRelationship[];
}

export interface PositionAssignment {
  id: string;
  positionId: string;
  positionName?: string;
  effectiveFrom: string;
  effectiveTo?: string;
  isPrimary: boolean;
  isActive: boolean;
}

export interface DepartmentAssignment {
  id: string;
  masterDepartmentId: string;
  effectiveFrom: string;
  isPrimary: boolean;
  allocationPercentage: number;
  isTemporary: boolean;
}

export interface ReportingRelationship {
  id: string;
  managerEmploymentId: string;
  managerPersonSearchName?: string;
  effectiveFrom: string;
  isActive: boolean;
}

export interface OrgChartNode {
  employmentId: string;
  employmentNumber: string;
  personSearchName: string;
  positionName?: string;
  reports: OrgChartNode[];
}

export interface PagedEmployments<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class EmploymentApiService extends BaseApiService {
  list(page = 1, pageSize = 20, organizationId?: string, hospitalId?: string): Observable<PagedEmployments<EmploymentListItem>> {
    return this.get<PagedEmployments<EmploymentListItem>>('/employment', { page, pageSize, organizationId, hospitalId });
  }

  search(q: string, page = 1, pageSize = 20): Observable<PagedEmployments<EmploymentListItem>> {
    return this.get<PagedEmployments<EmploymentListItem>>('/employment/search', { q, page, pageSize });
  }

  getById(id: string): Observable<EmploymentDetail> {
    return this.get<EmploymentDetail>(`/employment/${id}`);
  }

  getTimeline(id: string): Observable<{ id: string; occurredAt: string; eventType: string; summary: string }[]> {
    return this.get(`/employment/${id}/timeline`);
  }

  getHistory(id: string): Observable<{ id: string; occurredAt: string; eventType: string; reason?: string }[]> {
    return this.get(`/employment/${id}/history`);
  }

  getOrgChart(organizationId: string, hospitalId?: string): Observable<OrgChartNode> {
    return this.get<OrgChartNode>('/employment/org-chart', { organizationId, hospitalId });
  }

  getManagerTeam(managerEmploymentId: string): Observable<EmploymentListItem[]> {
    return this.get<EmploymentListItem[]>(`/employment/manager/${managerEmploymentId}/team`);
  }

  transfer(id: string, body: Record<string, unknown>): Observable<EmploymentDetail> {
    return this.post<EmploymentDetail>(`/employment/${id}/transfer`, body);
  }

  promote(id: string, body: Record<string, unknown>): Observable<EmploymentDetail> {
    return this.post<EmploymentDetail>(`/employment/${id}/promote`, body);
  }
}
