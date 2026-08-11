import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from '../../../shared/services/base-api.service';

export interface ExecutiveDashboard {
  organizationId: string;
  asOfUtc: string;
  headcount: number;
  openVacancies: number;
  trainingCompliancePercent: number;
  credentialRiskCount: number;
  criticalSkillGaps: number;
  committeeActionBacklog: number;
  availabilityCoveragePercent: number;
  growthRate?: number;
  attritionRate?: number;
  retentionRate?: number;
}

export interface HeadcountAnalytics {
  totalHeadcount: number;
  totalFte: number;
  byDepartment: Record<string, number>;
  trend: { asOfUtc: string; headcount: number }[];
}

export interface SkillMatrix {
  coveragePercent: number;
  rows: { skillName: string; currentLevel: string; gap?: string; isCritical: boolean }[];
  criticalGaps: { skillName: string; gapCount: number }[];
}

export interface TrainingMatrix {
  organizationCompliancePercent: number;
  rows: { courseName: string; status: string; isMandatory: boolean }[];
}

export interface CredentialDashboard {
  active: number;
  expiring: number;
  expired: number;
  renewalRequired: number;
  items: { name: string; status: string; personName: string }[];
}

export interface ForecastData {
  forecastType: string;
  version: number;
  dataPoints: { label: string; count: number }[];
}

export interface RecommendationList {
  items: { id: string; title: string; explanation: string; recommendationType: string; priority: string; confidence: number }[];
}

export interface WorkforceSearchResult {
  totalCount: number;
  hits: { personId: string; displayName: string; departmentName?: string }[];
}

@Injectable({ providedIn: 'root' })
export class WorkforceIntelligenceApiService extends BaseApiService {
  private base = '/api/v1/intelligence';

  getDashboard(organizationId: string): Observable<ExecutiveDashboard> {
    return this.get<ExecutiveDashboard>(`${this.base}/dashboard`, { organizationId })
      ;
  }

  getHeadcount(organizationId: string): Observable<HeadcountAnalytics> {
    return this.get<HeadcountAnalytics>(`${this.base}/headcount`, { organizationId })
      ;
  }

  getSkills(organizationId: string): Observable<SkillMatrix> {
    return this.get<SkillMatrix>(`${this.base}/skills`, { organizationId });
  }

  getCompetencies(organizationId: string): Observable<unknown> {
    return this.get<unknown>(`${this.base}/competencies`, { organizationId });
  }

  getTraining(organizationId: string): Observable<TrainingMatrix> {
    return this.get<TrainingMatrix>(`${this.base}/training`, { organizationId });
  }

  getLicenses(organizationId: string): Observable<CredentialDashboard> {
    return this.get<CredentialDashboard>(`${this.base}/licenses`, { organizationId });
  }

  getCertifications(organizationId: string): Observable<CredentialDashboard> {
    return this.get<CredentialDashboard>(`${this.base}/certifications`, { organizationId });
  }

  getForecast(organizationId: string, forecastType: string): Observable<ForecastData> {
    return this.get<ForecastData>(`${this.base}/forecast`, { organizationId, forecastType });
  }

  getRecommendations(organizationId: string): Observable<RecommendationList> {
    return this.get<RecommendationList>(`${this.base}/recommendations`, { organizationId });
  }

  search(organizationId: string, query: string): Observable<WorkforceSearchResult> {
    return this.get<WorkforceSearchResult>(`${this.base}/search`, { organizationId, query });
  }

  getOrganization(organizationId: string): Observable<unknown> {
    return this.get<unknown>(`${this.base}/organization`, { organizationId });
  }

  getResponsibilities(organizationId: string): Observable<unknown> {
    return this.get<unknown>(`${this.base}/responsibilities`, { organizationId });
  }

  getCommittees(organizationId: string): Observable<unknown> {
    return this.get<unknown>(`${this.base}/committees`, { organizationId });
  }

  getAvailability(organizationId: string): Observable<unknown> {
    return this.get<unknown>(`${this.base}/availability`, { organizationId });
  }

  refresh(organizationId: string): Observable<number> {
    return this.post<number>(`${this.base}/refresh?organizationId=${organizationId}`, {});
  }
}
