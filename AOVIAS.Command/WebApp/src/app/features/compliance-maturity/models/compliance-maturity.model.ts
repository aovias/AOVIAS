/**
 * Frontend models for the Compliance Maturity Dashboard.
 * These mirror the ComplianceMaturityDashboardDto shape returned by
 * GET /api/v1/compliance-maturity/dashboard via the Gateway.
 */

/** Matches the camelCase/lowercase values the .NET backend serializes for these enums. */

export type MaturityStage = 'core' | 'commitment' | 'achievement' | 'excellence';

export type MaturityStageStatus = 'completed' | 'inProgress' | 'available' | 'locked';

export type GapSeverity = 'critical' | 'major' | 'minor';

export type GapItemStatus = 'open' | 'inRemediation' | 'resolved';

export type RecommendationActionType =
  | 'ContinueCoreRemediation'
  | 'StartCommitmentAssessment'
  | 'ViewFullGapAnalysis'
  | 'ReviewCriticalGaps'
  | 'StartAchievementAssessment'
  | 'ViewReport'
  | 'StageUnlockRecommendation';

// ── Governance (Sprint 3 — H1B) ───────────────────────────────────────────────

export interface StageBlockingSummaryModel {
  stage: MaturityStage;
  stageLabel: string;
  openCriticalDeficiencies: number;
  missingEvidenceCount: number;
  verificationFailureCount: number;
  readinessGapPercent: number;
  remainingObligationsCount: number;
  blockingReasons: string[];
}

export interface AccreditationForecastModel {
  currentReadinessPercent: number;
  targetReadinessPercent: number;
  estimatedUnlockReadinessPercent: number;
  remainingEffortPercent: number;
  complianceMaturityIndex: number;
}

export interface UnlockAuditTimelineEntryModel {
  title: string;
  description: string;
  occurredAt: string;
  eventType: string;
}

export interface CapaGovernanceSummaryModel {
  capaEffectivenessRate: number;
  recurringIssues: number;
  pendingReviews: number;
  capaCompletionRate: number;
}

// ── CMI ──────────────────────────────────────────────────────────────────────

export interface ComplianceMaturityIndexModel {
  /** Weighted composite score 0–100. CMI = Core×0.4 + Commitment×0.3 + Achievement×0.2 + Excellence×0.1 */
  value: number;
  coreScore: number;
  commitmentScore: number;
  achievementScore: number;
  excellenceScore: number;
}

// ── Stage ─────────────────────────────────────────────────────────────────────

export interface MaturityStageModel {
  stage: MaturityStage;
  label: string;
  score: number;
  status: MaturityStageStatus;
  gapCount: number;
  unlockThreshold: number;
  unlockMessage: string;
  progressPercent?: number;
  readinessPercent?: number;
  unlockReadinessPercent?: number;
  blockingReasons?: string[];
  recommendations?: string[];
  nextMilestone?: string | null;
}

// ── Actual gaps (Core stage) ─────────────────────────────────────────────────

export interface GapItemModel {
  id: string;
  title: string;
  severity: GapSeverity;
  chapter: string;
  standard: string;
  status: GapItemStatus;
  classification?: 'complianceDeficiency';
}

export interface GapSummaryModel {
  readinessScore: number;
  openGapCount: number;
  criticalCount: number;
  majorCount: number;
  minorCount: number;
  items: GapItemModel[];
}

// ── Predicted gaps (Commitment / Achievement / Excellence) ───────────────────

export interface PredictedGapItemModel {
  title: string;
  riskCategory: string;
  chapter: string;
  classification?: 'predictedGap';
}

export interface PredictedGapSummaryModel {
  predictedGapCount: number;
  unlockCriteria: string;
  isLocked: boolean;
  items: PredictedGapItemModel[];
}

// ── Recommendations / CTAs ────────────────────────────────────────────────────

export interface JourneyRecommendationModel {
  title: string;
  description: string;
  priority: number;
  actionType: RecommendationActionType;
  isEnabled: boolean;
}

// ── Top-level dashboard model ─────────────────────────────────────────────────

export interface ComplianceMaturityDashboardModel {
  organizationId: string;
  organizationName: string;
  frameworkId: string;
  frameworkName: string;
  assessmentDate: string | null;
  complianceMaturityIndex: ComplianceMaturityIndexModel;
  currentStage: MaturityStage;
  stages: MaturityStageModel[];
  coreSummary: GapSummaryModel;
  commitmentSummary: PredictedGapSummaryModel;
  achievementSummary: PredictedGapSummaryModel;
  excellenceSummary: PredictedGapSummaryModel;
  nextActions: JourneyRecommendationModel[];
  activeStageBlocking?: StageBlockingSummaryModel | null;
  accreditationForecast?: AccreditationForecastModel;
  unlockAuditTimeline?: UnlockAuditTimelineEntryModel[];
  auditOriginDeficiencyCount?: number;
  capaGovernance?: CapaGovernanceSummaryModel;
}
