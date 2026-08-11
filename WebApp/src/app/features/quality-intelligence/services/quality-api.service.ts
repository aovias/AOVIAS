import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from '../../../shared/services/base-api.service';
import type {
  ClinicalIndicatorDetail,
  ClinicalIndicatorList,
  DepartmentScorecard,
  IndicatorQuery,
  QualityAnalytics,
  QualityBenchmarking,
  QualityDashboard,
  QualityExecutive,
  QualityTrends,
} from '../models/quality-indicator.models';

@Injectable({ providedIn: 'root' })
export class QualityApiService extends BaseApiService {
  getDashboard(): Observable<QualityDashboard> {
    return this.get('/quality/dashboard');
  }

  getIndicators(query: IndicatorQuery = {}): Observable<ClinicalIndicatorList> {
    return this.get('/quality/indicators', {
      page: query.page ?? 1,
      pageSize: query.pageSize ?? 25,
      sortDesc: query.sortDesc ?? true,
      ...(query.category ? { category: query.category } : {}),
      ...(query.department ? { department: query.department } : {}),
      ...(query.thresholdStatus ? { thresholdStatus: query.thresholdStatus } : {}),
      ...(query.trend ? { trend: query.trend } : {}),
      ...(query.search ? { search: query.search } : {}),
      ...(query.sortBy ? { sortBy: query.sortBy } : {}),
    });
  }

  getById(id: string): Observable<ClinicalIndicatorDetail> {
    return this.get(`/quality/indicators/${id}`);
  }

  patchThresholds(payload: {
    indicatorId: string;
    targetValue?: number;
    greenThreshold?: number;
    yellowThreshold?: number;
    redThreshold?: number;
    criticalThreshold?: number;
    lowerIsBetter?: boolean;
    reason?: string;
  }): Observable<ClinicalIndicatorDetail> {
    return super.patch('/quality/thresholds', payload);
  }

  getScorecards(): Observable<DepartmentScorecard[]> {
    return this.get('/quality/scorecards');
  }

  getTrends(period?: string, indicatorId?: string): Observable<QualityTrends> {
    return this.get('/quality/trends', {
      ...(period ? { period } : {}),
      ...(indicatorId ? { indicatorId } : {}),
    });
  }

  getBenchmarking(comparisonPeriod?: string): Observable<QualityBenchmarking> {
    return this.get('/quality/benchmarking', comparisonPeriod ? { comparisonPeriod } : {});
  }

  getExecutive(): Observable<QualityExecutive> {
    return this.get('/quality/executive');
  }

  getAnalytics(): Observable<QualityAnalytics> {
    return this.get('/quality/analytics');
  }
}
