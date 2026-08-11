/**
 * DTO contracts for the Compliance Command Center API
 * (GET /api/v1/control-center/compliance/*). Field casing mirrors the backend's camelCase JSON.
 * These are the wire models; the facade maps them to the existing child component view models.
 */

export interface ComplianceHeaderDto {
  isLive: boolean;
  lastUpdated: string;
  organizationId: string;
  organizationName: string;
}

export interface ComplianceInsightDto {
  /** ComplianceDrop | ComplianceImprovement | MissingEvidence | CriticalTasks | AuditRisk | OverdueCapa | PolicyGap | DocumentExpiry */
  type: string;
  /** critical | warning | success | info */
  severity: string;
  message: string;
  metric?: string | null;
}

export interface ComplianceIntelligenceDto {
  insights: ComplianceInsightDto[];
}

export interface RecommendedActionDto {
  id: string;
  title: string;
  description: string;
  priority: string;
  impactPercent: number;
  actionType: string;
  entityId?: string | null;
}

export interface ComplianceCommandCenterKpiDto {
  complianceReadiness: number;
  auditConfidence: number;
  openFindings: number;
  criticalTasks: number;
  evidenceCoverage: number;
  upcomingAudits: number;

  complianceReadinessTrend: number;
  auditConfidenceTrend: number;
  openFindingsTrend: number;
  criticalTasksTrend: number;
  evidenceCoverageTrend: number;
  upcomingAuditsTrend: number;

  complianceReadinessSparkline: number[];
  auditConfidenceSparkline: number[];
  evidenceCoverageSparkline: number[];
}

export interface HighRiskChapterDto {
  chapter: string;
  complianceScore: number;
  riskLevel: string;
  openIssues: number;
}

export interface DepartmentRiskDto {
  department: string;
  riskScore: number;
  riskLevel: string;
}

export interface ActionCenterItemDto {
  id: string;
  title: string;
  complianceImpact: string;
  owner: string;
  department: string;
  dueDate?: string | null;
  priority: string;
  status: string;
}

export interface ComplianceTrendPointDto {
  date: string;
  compliance: number;
  target: number;
}

export interface ChapterComplianceDto {
  chapter: string;
  compliance: number;
}

export interface EvidenceCoverageDto {
  approved: number;
  pending: number;
  rejected: number;
  total: number;
  coveragePercent: number;
}

export interface AuditReadinessDto {
  confidenceScore: number;
  status: string;
  criticalFindings: number;
  correctiveActionsPending: number;
  clausesMissingEvidence: number;
  auditPassProbability: number;
  thresholdPercent: number;
  recommendation: string;
}

export interface ActivityEventDto {
  id: string;
  type: string;
  title: string;
  description: string;
  user: string;
  timestamp: string;
  department?: string | null;
}

export interface AlertActionDto {
  label: string;
  type: string;
  action: string;
}

export interface AlertDto {
  id: string;
  severity: string;
  title: string;
  description: string;
  timestamp: string;
  actions: AlertActionDto[];
}

export interface ControlCenterOverviewDto {
  header: ComplianceHeaderDto;
  intelligence: ComplianceIntelligenceDto;
  recommendedActions: RecommendedActionDto[];
  kpis: ComplianceCommandCenterKpiDto;
  highRiskChapters: HighRiskChapterDto[];
  departmentRisks: DepartmentRiskDto[];
  actionCenter: ActionCenterItemDto[];
  trend: ComplianceTrendPointDto[];
  chapterCompliance: ChapterComplianceDto[];
  evidenceCoverage: EvidenceCoverageDto;
  auditReadiness: AuditReadinessDto;
  recentActivity: ActivityEventDto[];
  alerts: AlertDto[];
}

export interface ControlCenterQuery {
  hospitalId?: string | null;
  frameworkId?: string | null;
}
