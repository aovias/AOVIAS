import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { BaseApiService } from '../../../shared/services/base-api.service';

export interface OeOwnershipMatrixItem {
  ownershipId?: string;
  objectiveElementId: string;
  objectiveElementCode: string;
  objectiveElementTitle: string;
  frameworkId: string;
  departmentId?: string;
  departmentName?: string;
  primaryOwnerUserId?: string;
  dueDate?: string;
  status: string;
  journeyStatus?: string;
}

export interface UpsertOeOwnershipRequest {
  organizationId: string;
  frameworkId: string;
  objectiveElementId: string;
  departmentId: string;
  primaryOwnerUserId: string;
  dueDate?: string | null;
  status?: string;
}

export interface HospitalFrameworkOption {
  frameworkId: string;
  frameworkName: string;
  status?: string;
  enrolledOn?: string;
  expiryDate?: string;
}

@Injectable({ providedIn: 'root' })
export class OeOwnershipApiService extends BaseApiService {
  private unwrap<T>(r: T | { data: T }): T {
    return r && typeof r === 'object' && 'data' in (r as object)
      ? (r as { data: T }).data
      : (r as T);
  }

  listHospitalFrameworks(hospitalId: string): Observable<HospitalFrameworkOption[]> {
    return this.get<HospitalFrameworkOption[] | { data: HospitalFrameworkOption[] }>(
      `/hospitals/${hospitalId}/frameworks`,
    ).pipe(
      map((r) => {
        const list = this.unwrap(r) ?? [];
        return list.map((f) => ({
          frameworkId: f.frameworkId || (f as { FrameworkId?: string }).FrameworkId || '',
          frameworkName: f.frameworkName || (f as { FrameworkName?: string }).FrameworkName || 'Framework',
          status: f.status,
          enrolledOn: f.enrolledOn,
          expiryDate: f.expiryDate,
        }));
      }),
    );
  }

  listMatrix(hospitalId: string, frameworkId?: string): Observable<OeOwnershipMatrixItem[]> {
    return this.get<OeOwnershipMatrixItem[] | { data: OeOwnershipMatrixItem[] }>(
      `/hospitals/${hospitalId}/oe-ownership`,
      { frameworkId },
    ).pipe(map((r) => this.unwrap(r) ?? []));
  }

  upsert(hospitalId: string, body: UpsertOeOwnershipRequest): Observable<OeOwnershipMatrixItem> {
    return this.put<OeOwnershipMatrixItem | { data: OeOwnershipMatrixItem }>(
      `/hospitals/${hospitalId}/oe-ownership`,
      body,
    ).pipe(map((r) => this.unwrap(r)));
  }

  clear(hospitalId: string, objectiveElementId: string): Observable<void> {
    return this.delete<void>(`/hospitals/${hospitalId}/oe-ownership/${objectiveElementId}`);
  }
}
