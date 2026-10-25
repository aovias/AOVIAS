import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from '../../shared/services/base-api.service';

export interface ComplianceSearchHit {
  type: string;
  id: string;
  title: string;
  status: string;
  hospitalId?: string | null;
  department?: string | null;
  owner?: string | null;
  href?: string | null;
  subtitle?: string | null;
}

export interface ComplianceSearchResponse {
  query: string;
  totalResults: number;
  source: string;
  results: ComplianceSearchHit[];
}

@Injectable({ providedIn: 'root' })
export class ComplianceSearchApiService extends BaseApiService {
  search(params: {
    q: string;
    hospitalId?: string | null;
    status?: string;
    owner?: string;
    department?: string;
  }): Observable<ComplianceSearchResponse> {
    return this.get<ComplianceSearchResponse>('/compliance/search', {
      q: params.q,
      hospitalId: params.hospitalId || undefined,
      status: params.status,
      owner: params.owner,
      department: params.department,
    });
  }
}
