import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from '../../../shared/services/base-api.service';

export interface PlatformActivityEntry {
  id: string;
  timestampUtc: string;
  category: string;
  action: string;
  module: string;
  service: string;
  userId?: string;
  target?: string;
  status: string;
  ipAddress?: string;
  correlationId?: string;
  previousValue?: string;
  newValue?: string;
  detailsJson?: string;
}

export interface PlatformActivitySummary {
  administrativeActions: number;
  securityEvents: number;
  configurationChanges: number;
  userManagementEvents: number;
  integrationEvents: number;
  notificationEvents: number;
  recentErrors: number;
}

export interface PlatformActivityResponse {
  summary: PlatformActivitySummary;
  items: PlatformActivityEntry[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface PlatformActivityQuery {
  search?: string;
  from?: string;
  to?: string;
  module?: string;
  category?: string;
  action?: string;
  userId?: string;
  status?: string;
  service?: string;
  sort?: 'asc' | 'desc';
  page?: number;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class AdministrationAuditApiService extends BaseApiService {
  getActivity(query: PlatformActivityQuery = {}): Observable<PlatformActivityResponse> {
    const params = new URLSearchParams();
    if (query.search) params.set('search', query.search);
    if (query.from) params.set('from', query.from);
    if (query.to) params.set('to', query.to);
    if (query.module) params.set('module', query.module);
    if (query.category) params.set('category', query.category);
    if (query.action) params.set('action', query.action);
    if (query.userId) params.set('userId', query.userId);
    if (query.status) params.set('status', query.status);
    if (query.service) params.set('service', query.service);
    if (query.sort) params.set('sort', query.sort);
    params.set('page', String(query.page ?? 1));
    params.set('pageSize', String(query.pageSize ?? 25));
    const qs = params.toString();
    return this
      .get<PlatformActivityResponse>(`/api/v1/administration/audit/activity${qs ? `?${qs}` : ''}`);
  }
}
