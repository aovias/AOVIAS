export interface AnalyticsDeepLink {
  label: string;
  route: string;
  queryParams?: Record<string, string>;
}

export interface AnalyticsMetricCard {
  key: string;
  label: string;
  value?: number | null;
  unit?: string | null;
  source?: string | null;
  deepLink?: AnalyticsDeepLink | null;
}

export interface AnalyticsBreakdownItem {
  label: string;
  count: number;
  deepLink?: AnalyticsDeepLink | null;
}

export interface TrendPoint {
  label: string;
  value: number;
}

export interface TrendSeries {
  metricKey: string;
  label: string;
  granularity: string;
  points: TrendPoint[];
}

export interface AnalyticsContext {
  organizationId: string;
  hospitalId: string;
  frameworkId: string;
  frameworkName?: string | null;
  generatedAt: string;
}

export interface AnalyticsOverview {
  context: AnalyticsContext;
  readinessScore: number;
  complianceMaturityIndex: number;
  openDeficiencies: number;
  criticalDeficiencies: number;
  capaEffectivenessRate: number;
  slaHealthScore: number;
  escalationRate: number;
  recurringIssues: number;
  pendingReviews: number;
  currentStage: string;
  kpiCards: AnalyticsMetricCard[];
}

export interface AnalyticsReadinessSection {
  score: number;
  source: string;
  deepLink?: AnalyticsDeepLink | null;
}

export interface ExecutiveAnalytics {
  context: AnalyticsContext;
  complianceReadiness: AnalyticsReadinessSection;
  accreditationReadiness: AnalyticsReadinessSection;
  departmentPerformance: AnalyticsBreakdownItem[];
  capaPerformance: {
    capaEffectivenessRate: number;
    recurringIssues: number;
    pendingReviews: number;
    capaCompletionRate: number;
  };
  slaHealth: {
    openCriticalEscalations: number;
    overdueDeficiencies: number;
    slaHealthScore: number;
    departmentsAtRisk: string[];
  };
  riskExposure: {
    criticalOpenDeficiencies: number;
    highRiskDeficiencies: number;
    criticalRiskCount: number;
    deepLink?: AnalyticsDeepLink | null;
  };
  escalationExposure: {
    openEscalations: number;
    criticalEscalations: number;
    escalationRate: number;
    deepLink?: AnalyticsDeepLink | null;
  };
}

export interface CapaAgingBuckets {
  within30: number;
  days31To60: number;
  days61To90: number;
  over90: number;
}

export interface OperationalOverdueWork {
  overdueCapas: number;
  overdueTasks: number;
  overdueDeficiencies: number;
  departmentOverdueCapas: AnalyticsBreakdownItem[];
}

export interface OperationalAnalytics {
  context: AnalyticsContext;
  deficiencyFunnel: {
    open: number;
    awaitingAssignment: number;
    inProgress: number;
    awaitingEvidence: number;
    verificationPending: number;
    verified: number;
    closed: number;
    deepLink?: AnalyticsDeepLink | null;
  };
  taskThroughput: {
    total: number;
    notStarted: number;
    inProgress: number;
    blocked: number;
    completed: number;
    overdue: number;
  };
  evidencePipeline: {
    total: number;
    approved: number;
    pendingReview: number;
    rejected: number;
    missing: number;
  };
  verificationBacklog: number;
  assignmentLoad: number;
  recurringIssues: number;
  rootCauseTrend: AnalyticsBreakdownItem[];
  deepLinks: AnalyticsDeepLink[];
  /** Phase 2 Sprint 6 operational KPIs */
  capaAging?: CapaAgingBuckets;
  averageOpenCapaAgeDays?: number;
  averageResolutionHours?: number;
  averageResolutionDays?: number;
  auditCompletionRate?: number;
  auditSessionsTotal?: number;
  auditSessionsClosedOrSubmitted?: number;
  departmentPerformance?: AnalyticsBreakdownItem[];
  slaComplianceRate?: number;
  slaHealthScore?: number;
  overdueWork?: OperationalOverdueWork;
}

export interface RcaAnalytics {
  context: AnalyticsContext;
  topRootCauses: AnalyticsBreakdownItem[];
  rootCauseByDepartment: AnalyticsBreakdownItem[];
  rootCauseByChapter: AnalyticsBreakdownItem[];
  rootCauseByStage: AnalyticsBreakdownItem[];
  rootCauseTrend: TrendPoint[];
  recurrenceTrend: TrendPoint[];
}

export interface CapaAnalytics {
  context: AnalyticsContext;
  capaEffectivenessRate: number;
  averageClosureDays: number;
  pendingReviews: number;
  recurringIssues: number;
  departmentCapaPerformance: AnalyticsBreakdownItem[];
  deepLink?: AnalyticsDeepLink | null;
}

export interface SlaAnalytics {
  context: AnalyticsContext;
  slaHealthScore: number;
  breachRate: number;
  escalationRate: number;
  averageResolutionHours: number;
  criticalDeficiencyResolutionOpen: number;
  departmentSlaCompliance: AnalyticsBreakdownItem[];
  deepLink?: AnalyticsDeepLink | null;
}

export interface DepartmentAnalyticsRow {
  department: string;
  readinessScore: number;
  openDeficiencies: number;
  capaOpen: number;
  slaAtRisk: number;
  escalations: number;
  taskBacklog: number;
  evidencePending: number;
  verificationPending: number;
  deepLink?: AnalyticsDeepLink | null;
}

export interface DepartmentAnalytics {
  context: AnalyticsContext;
  departments: DepartmentAnalyticsRow[];
}

export interface AnalyticsTrends {
  context: AnalyticsContext;
  series: TrendSeries[];
}

export type AnalyticsSection =
  | 'overview'
  | 'executive'
  | 'operational'
  | 'rca'
  | 'capa'
  | 'sla'
  | 'departments';
