import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from '../../../shared/services/base-api.service';
import {
  AnalyticsOverview,
  AnalyticsTrends,
  CapaAnalytics,
  DepartmentAnalytics,
  ExecutiveAnalytics,
  OperationalAnalytics,
  RcaAnalytics,
  SlaAnalytics,
} from '../models/analytics.model';

@Injectable({ providedIn: 'root' })
export class AnalyticsApiService extends BaseApiService {
  getOverview(frameworkId?: string): Observable<AnalyticsOverview> {
    return this.get<AnalyticsOverview>('/analytics/overview', frameworkId ? { frameworkId } : undefined);
  }

  getExecutive(frameworkId?: string): Observable<ExecutiveAnalytics> {
    return this.get<ExecutiveAnalytics>('/analytics/executive', frameworkId ? { frameworkId } : undefined);
  }

  getOperational(frameworkId?: string): Observable<OperationalAnalytics> {
    return this.get<OperationalAnalytics>('/analytics/operational', frameworkId ? { frameworkId } : undefined);
  }

  getRca(frameworkId?: string): Observable<RcaAnalytics> {
    return this.get<RcaAnalytics>('/analytics/rca', frameworkId ? { frameworkId } : undefined);
  }

  getCapa(frameworkId?: string): Observable<CapaAnalytics> {
    return this.get<CapaAnalytics>('/analytics/capa', frameworkId ? { frameworkId } : undefined);
  }

  getSla(frameworkId?: string): Observable<SlaAnalytics> {
    return this.get<SlaAnalytics>('/analytics/sla', frameworkId ? { frameworkId } : undefined);
  }

  getDepartments(frameworkId?: string): Observable<DepartmentAnalytics> {
    return this.get<DepartmentAnalytics>('/analytics/departments', frameworkId ? { frameworkId } : undefined);
  }

  getTrends(params?: {
    frameworkId?: string;
    metric?: string;
    from?: string;
    to?: string;
    granularity?: string;
  }): Observable<AnalyticsTrends> {
    return this.get<AnalyticsTrends>('/analytics/trends', params);
  }
}
