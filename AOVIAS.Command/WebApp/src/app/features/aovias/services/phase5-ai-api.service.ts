import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from '../../../shared/services/base-api.service';
import { ComplianceContextService } from '../../../core/services/compliance-context.service';

export interface AiSourceRef {
  sourceType: string;
  sourceId: string;
  summary?: string | null;
  relevanceScore: number;
}

export interface RiskClusterDto {
  theme: string;
  summary: string;
  severity: string;
  signalCount: number;
  mitigations: string[];
  citations: AiSourceRef[];
  confidence: number;
}

export interface EmergingRiskDto {
  title: string;
  reason: string;
  severity: string;
  confidence: number;
}

export interface RiskIntelligenceResult {
  hospitalId: string;
  frameworkId?: string | null;
  clusters: RiskClusterDto[];
  emergingRisks: EmergingRiskDto[];
  recommendedMitigations: string[];
  usedFallbackContext?: boolean;
}

export interface ReadinessForecastDto {
  currentReadinessPercent: number;
  forecastReadinessPercent: number;
  horizon: string;
  trend: string;
  narrative: string;
  confidence: number;
}

export interface PredictiveOverviewDto {
  horizon: string;
  readiness: ReadinessForecastDto;
  delays: {
    capaDelayProbability: number;
    documentExpiryRisk: number;
    trainingExpiryRisk: number;
    narrative: string;
  };
}

export interface AceInsightDto {
  id: string;
  category: string;
  title: string;
  summary: string;
  confidence: number;
  priority: number;
  status: string;
  suggestedActions: string[];
  createdAtUtc: string;
}

export interface AiReportGenerateResult {
  reportType: string;
  title: string;
  markdown: string;
  recommendations: string[];
  actionItems: string[];
  notes: string;
}

@Injectable({ providedIn: 'root' })
export class Phase5AiApiService extends BaseApiService {
  private readonly complianceContext = inject(ComplianceContextService);

  analyzeRiskIntelligence(payload: {
    hospitalId?: string;
    frameworkId?: string | null;
    maxClusters?: number;
  } = {}): Observable<RiskIntelligenceResult> {
    const hospitalId = payload.hospitalId ?? this.complianceContext.getHospitalId() ?? undefined;
    const frameworkId = payload.frameworkId ?? this.complianceContext.getFrameworkId() ?? undefined;
    return this.post('/ai/risk-intelligence/analyze', {
      ...payload,
      hospitalId,
      frameworkId,
      maxClusters: payload.maxClusters ?? 8,
    });
  }

  predictOverview(horizon: 'weekly' | 'monthly' | 'quarterly' = 'monthly'): Observable<PredictiveOverviewDto> {
    const hospitalId = this.complianceContext.getHospitalId() ?? undefined;
    const frameworkId = this.complianceContext.getFrameworkId() ?? undefined;
    return this.get('/ai/predict/overview', {
      horizon,
      ...(hospitalId ? { hospitalId } : {}),
      ...(frameworkId ? { frameworkId } : {}),
    });
  }

  listAceInsights(status = 'Open', take = 8): Observable<AceInsightDto[]> {
    return this.get('/ai/ace/insights', { status, take });
  }

  acknowledgeAceInsight(insightId: string): Observable<AceInsightDto> {
    return this.post(`/ai/ace/insights/${insightId}/acknowledge`, {});
  }

  generateReport(reportType: string, horizon = 'monthly'): Observable<AiReportGenerateResult> {
    return this.post('/ai/reports/generate', { reportType, horizon });
  }
}
