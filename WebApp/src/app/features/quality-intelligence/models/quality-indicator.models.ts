export interface QualityDashboard {
  overallQualityScore: number;
  indicatorsWithinTarget: number;
  warningIndicators: number;
  criticalIndicators: number;
  improvingTrends: number;
  decliningTrends: number;
  activeIndicators: number;
  patientSafetyScore: number;
  accreditationRiskScore: number;
  capaEffectivenessScore: number;
  byCategory: QualityCountByLabel[];
  byDepartment: QualityCountByLabel[];
  byThresholdStatus: QualityCountByLabel[];
}

export interface QualityCountByLabel {
  label: string;
  count: number;
  value?: number;
}

export interface ClinicalIndicatorListItem {
  id: string;
  indicatorCode: string;
  name: string;
  category: string;
  department: string;
  measurementUnit: string;
  frequency: string;
  currentValue?: number;
  targetValue?: number;
  thresholdStatus: string;
  trend: string;
  riskLevel: string;
  status: string;
  ownerName?: string;
  lastCalculatedAt?: string;
}

export interface ClinicalIndicatorList {
  items: ClinicalIndicatorListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface ClinicalIndicatorDetail extends ClinicalIndicatorListItem {
  description: string;
  calculationMethod: string;
  numeratorDefinition?: string;
  denominatorDefinition?: string;
  formulaExpression?: string;
  currentNumerator?: number;
  currentDenominator?: number;
  greenThreshold?: number;
  yellowThreshold?: number;
  redThreshold?: number;
  criticalThreshold?: number;
  lowerIsBetter: boolean;
  frameworkMapping: string;
  frameworkReference?: string;
  objectiveElementId?: string;
  dataSource?: string;
  comments?: string;
  trendHistory: IndicatorMeasurement[];
  thresholdHistory: IndicatorThresholdHistory[];
  lifecycleHistory: IndicatorAuditTrail[];
  relatedIncidents: QualityLinkedEntity[];
  relatedFindings: QualityLinkedEntity[];
  relatedDeficiencies: QualityLinkedEntity[];
  relatedCapas: QualityLinkedEntity[];
  relatedDocuments: QualityLinkedEntity[];
  investigationRecommendations: QualityInvestigationRecommendation[];
  complianceContext?: QualityComplianceContext;
}

export interface IndicatorMeasurement {
  id: string;
  periodStart: string;
  periodEnd: string;
  frequency: string;
  numerator?: number;
  denominator?: number;
  value: number;
  calculatedAt: string;
}

export interface IndicatorThresholdHistory {
  id: string;
  targetValue?: number;
  greenThreshold?: number;
  yellowThreshold?: number;
  redThreshold?: number;
  criticalThreshold?: number;
  lowerIsBetter: boolean;
  reason?: string;
  changedByName?: string;
  effectiveFrom: string;
}

export interface IndicatorAuditTrail {
  action: string;
  detail?: string;
  actorName?: string;
  occurredAt: string;
}

export interface QualityLinkedEntity {
  id: string;
  label: string;
  subLabel?: string;
  deepLink?: string;
}

export interface QualityInvestigationRecommendation {
  recommendationType: string;
  title: string;
  rationale: string;
  deepLink?: string;
  isAdvisory: boolean;
}

export interface QualityComplianceContext {
  objectiveElementId?: string;
  objectiveElementName?: string;
  clauseReference?: string;
  readinessImpact?: number;
  verificationStatus?: string;
}

export interface DepartmentScorecard {
  department: string;
  qualityScore: number;
  clinicalIndicatorScore: number;
  incidentRate: number;
  capaCompletionPercent: number;
  slaHealthPercent: number;
  evidenceCompletenessPercent: number;
  documentationHealthPercent: number;
  indicatorsWithinTarget: number;
  warningIndicators: number;
  criticalIndicators: number;
  executiveSummary: string;
}

export interface QualityTrends {
  period: string;
  series: QualityTrendSeries[];
}

export interface QualityTrendSeries {
  indicatorId: string;
  indicatorCode: string;
  name: string;
  trend: string;
  points: IndicatorMeasurement[];
  movingAverage?: number;
}

export interface QualityBenchmarking {
  comparisonPeriod: string;
  items: QualityBenchmarkItem[];
  departmentComparison: QualityDepartmentComparison[];
}

export interface QualityBenchmarkItem {
  indicatorId: string;
  indicatorCode: string;
  name: string;
  currentValue?: number;
  previousValue?: number;
  changePercent?: number;
  targetValue?: number;
  trend: string;
}

export interface QualityDepartmentComparison {
  department: string;
  qualityScore: number;
  averageIndicatorValue: number;
  indicatorCount: number;
}

export interface QualityExecutive {
  topRisks: QualityExecutiveRisk[];
  topImprovements: QualityExecutiveImprovement[];
  departmentsNeedingAttention: string[];
  indicatorsThreateningAccreditation: string[];
  recurringProblems: string[];
  improvingDepartments: string[];
  monthlyQualitySummary: string;
  overallQualityScore: number;
  accreditationRiskScore: number;
}

export interface QualityExecutiveRisk {
  title: string;
  department: string;
  severity: string;
  deepLink?: string;
}

export interface QualityExecutiveImprovement {
  title: string;
  department: string;
  changePercent: number;
  deepLink?: string;
}

export interface QualityAnalytics {
  overallQualityScore: number;
  accreditationRiskScore: number;
  journeyReadiness: number;
  capaEffectiveness: number;
  indicatorHealth: QualityCountByLabel[];
  departmentHeatmap: QualityCountByLabel[];
  qualityTrends: QualityTrendSeries[];
  analyticsSource: string;
}

export interface IndicatorQuery {
  category?: string;
  department?: string;
  thresholdStatus?: string;
  trend?: string;
  search?: string;
  sortBy?: string;
  sortDesc?: boolean;
  page?: number;
  pageSize?: number;
}

export function formatQualityLabel(value: string): string {
  if (!value) return '—';
  return value.replace(/([A-Z])/g, ' $1').replace(/^./, (s) => s.toUpperCase()).trim();
}

export function thresholdClass(status: string): string {
  const s = status?.toLowerCase() ?? '';
  if (s === 'green') return 'sev--low';
  if (s === 'yellow') return 'sev--moderate';
  if (s === 'red') return 'sev--major';
  if (s === 'critical') return 'sev--critical';
  return 'badge';
}
