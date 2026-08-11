import { Injectable, inject } from '@angular/core';
import { HttpContext } from '@angular/common/http';
import { Observable } from 'rxjs';
import { BaseApiService } from '../../../shared/services/base-api.service';
import { ComplianceContextService } from '../../../core/services/compliance-context.service';
import {
  BACKGROUND_REQUEST,
  DASHBOARD_HTTP_CATEGORY,
  SKIP_LOADING_OVERLAY,
} from '../../../core/performance/http-context.tokens';
import {
  ActionCenterItemDto,
  AlertDto,
  AuditReadinessDto,
  ChapterComplianceDto,
  ComplianceCommandCenterKpiDto,
  ComplianceHeaderDto,
  ComplianceIntelligenceDto,
  ComplianceTrendPointDto,
  ControlCenterOverviewDto,
  ControlCenterQuery,
  DepartmentRiskDto,
  EvidenceCoverageDto,
  HighRiskChapterDto,
  RecommendedActionDto,
  ActivityEventDto,
} from '../models/control-center.models';

/**
 * Talks to the Compliance Command Center API through the Gateway
 * (`/api/v1/control-center/compliance/*`). Scope (hospital/framework) is resolved from
 * {@link ComplianceContextService} with an org-wide fallback when unset.
 */
@Injectable({ providedIn: 'root' })
export class ControlCenterService extends BaseApiService {
  private readonly context = inject(ComplianceContextService);
  private readonly base = '/control-center/compliance';

  getOverview(query?: ControlCenterQuery): Observable<ControlCenterOverviewDto> {
    const context = new HttpContext()
      .set(DASHBOARD_HTTP_CATEGORY, 'control-center')
      .set(BACKGROUND_REQUEST, false)
      .set(SKIP_LOADING_OVERLAY, false);
    return this.get<ControlCenterOverviewDto>(`${this.base}/overview`, this.params(query), { context });
  }

  /** Background refresh — no global loading overlay. */
  getOverviewBackground(query?: ControlCenterQuery): Observable<ControlCenterOverviewDto> {
    const context = new HttpContext()
      .set(DASHBOARD_HTTP_CATEGORY, 'control-center')
      .set(BACKGROUND_REQUEST, true)
      .set(SKIP_LOADING_OVERLAY, true);
    return this.get<ControlCenterOverviewDto>(`${this.base}/overview`, this.params(query), { context });
  }

  getHeader(query?: ControlCenterQuery): Observable<ComplianceHeaderDto> {
    return this.get<ComplianceHeaderDto>(`${this.base}/header`, this.params(query));
  }

  getIntelligence(query?: ControlCenterQuery): Observable<ComplianceIntelligenceDto> {
    return this.get<ComplianceIntelligenceDto>(`${this.base}/intelligence`, this.params(query));
  }

  getRecommendedActions(query?: ControlCenterQuery): Observable<RecommendedActionDto[]> {
    return this.get<RecommendedActionDto[]>(`${this.base}/actions`, this.params(query));
  }

  getKpis(query?: ControlCenterQuery): Observable<ComplianceCommandCenterKpiDto> {
    return this.get<ComplianceCommandCenterKpiDto>(`${this.base}/kpis`, this.params(query));
  }

  getHighRiskChapters(query?: ControlCenterQuery): Observable<HighRiskChapterDto[]> {
    return this.get<HighRiskChapterDto[]>(`${this.base}/high-risk-chapters`, this.params(query));
  }

  getDepartmentRisk(query?: ControlCenterQuery): Observable<DepartmentRiskDto[]> {
    return this.get<DepartmentRiskDto[]>(`${this.base}/department-risk`, this.params(query));
  }

  getActionCenter(query?: ControlCenterQuery): Observable<ActionCenterItemDto[]> {
    return this.get<ActionCenterItemDto[]>(`${this.base}/action-center`, this.params(query));
  }

  getTrend(query?: ControlCenterQuery): Observable<ComplianceTrendPointDto[]> {
    return this.get<ComplianceTrendPointDto[]>(`${this.base}/trend`, this.params(query));
  }

  getChapterCompliance(query?: ControlCenterQuery): Observable<ChapterComplianceDto[]> {
    return this.get<ChapterComplianceDto[]>(`${this.base}/chapter-compliance`, this.params(query));
  }

  getEvidenceCoverage(query?: ControlCenterQuery): Observable<EvidenceCoverageDto> {
    return this.get<EvidenceCoverageDto>(`${this.base}/evidence-coverage`, this.params(query));
  }

  getAuditReadiness(query?: ControlCenterQuery): Observable<AuditReadinessDto> {
    return this.get<AuditReadinessDto>(`${this.base}/audit-readiness`, this.params(query));
  }

  getActivityStream(type?: string, query?: ControlCenterQuery): Observable<ActivityEventDto[]> {
    return this.get<ActivityEventDto[]>(`${this.base}/activity-stream`, {
      ...this.params(query),
      ...(type && type !== 'all' ? { type } : {}),
    });
  }

  getAlerts(query?: ControlCenterQuery): Observable<AlertDto[]> {
    return this.get<AlertDto[]>(`${this.base}/alerts`, this.params(query));
  }

  /** Resolves request scope params, falling back to the shared compliance context. */
  private params(query?: ControlCenterQuery): Record<string, string> {
    const hospitalId = query?.hospitalId ?? this.context.getHospitalId();
    const frameworkId = query?.frameworkId ?? this.context.getFrameworkId();
    const params: Record<string, string> = {};
    if (hospitalId) {
      params['hospitalId'] = hospitalId;
    }
    if (frameworkId) {
      params['frameworkId'] = frameworkId;
    }
    return params;
  }
}
