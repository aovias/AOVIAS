import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from '../../../shared/services/base-api.service';
import type {
  CreateRiskRequest,
  RiskDetail,
  RiskList,
  RiskMatrix,
  RiskQuery,
  UpdateRiskRequest,
} from '../models/risk.models';

@Injectable({ providedIn: 'root' })
export class RiskApiService extends BaseApiService {
  private readonly base = '/risks';

  list(query: RiskQuery = {}): Observable<RiskList> {
    return this.get(this.base, {
      page: query.page ?? 1,
      pageSize: query.pageSize ?? 25,
      ...(query.status ? { status: query.status } : {}),
      ...(query.category ? { category: query.category } : {}),
      ...(query.search ? { search: query.search } : {}),
      ...(query.ownerUserId ? { ownerUserId: query.ownerUserId } : {}),
      ...(query.departmentId ? { departmentId: query.departmentId } : {}),
      ...(query.reviewDueOnly != null ? { reviewDueOnly: query.reviewDueOnly } : {}),
    });
  }

  getById(id: string): Observable<RiskDetail> {
    return this.get(`${this.base}/${id}`);
  }

  create(payload: CreateRiskRequest): Observable<RiskDetail> {
    return this.post(this.base, payload);
  }

  update(id: string, payload: UpdateRiskRequest): Observable<RiskDetail> {
    return this.put(`${this.base}/${id}`, payload);
  }

  matrix(useResidual = false): Observable<RiskMatrix> {
    return this.get(`${this.base}/matrix`, { useResidual });
  }
}
