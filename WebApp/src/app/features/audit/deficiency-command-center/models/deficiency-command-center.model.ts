/** Sprint 6 H5 + Sprint 8 — Deficiency Command Center API models */

export type DeficiencySeverity = 'Critical' | 'Major' | 'Moderate' | 'Minor';

export type DeficiencyStatus =
  | 'Identified'
  | 'Assigned'
  | 'InProgress'
  | 'EvidenceSubmitted'
  | 'ReviewPending'
  | 'Verified'
  | 'Closed'
  | 'Cancelled';

export type DeficiencyLifecycleStatus =
  | 'Identified'
  | 'Assigned'
  | 'In Progress'
  | 'Awaiting Evidence'
  | 'Verification Pending'
  | 'Verified'
  | 'Closed'
  | 'Cancelled';

export type DeficiencyRiskLevel = 'Low' | 'Medium' | 'High' | 'Critical';

export type EffectivenessReviewStatus =
  | 'NotRequired'
  | 'Pending'
  | 'Scheduled'
  | 'Effective'
  | 'NotEffective';

export type CapaActionStatus = 'NotStarted' | 'InProgress' | 'Completed';

export interface CapaLifecycleStep {
  key: string;
  label: string;
  isComplete: boolean;
  isCurrent: boolean;
}

export type RootCauseCategory =
  | 'PolicyMissing'
  | 'EvidenceMissing'
  | 'TrainingMissing'
  | 'ProcessFailure'
  | 'DocumentationIssue'
  | 'VerificationFailure'
  | 'Other';

export interface DeficiencyCountByLabel {
  label: string;
  count: number;
}

export interface DeficiencyCommandCenterDashboard {
  totalDeficiencies: number;
  criticalDeficiencies: number;
  majorDeficiencies: number;
  moderateDeficiencies: number;
  minorDeficiencies: number;
  openDeficiencies: number;
  resolvedDeficiencies: number;
  blockingDeficiencies: number;
  predictedGaps: number;
  auditFindings: number;
  auditOriginDeficiencies: number;
  assessmentOriginDeficiencies: number;
  manualOriginDeficiencies: number;
  awaitingAssignment: number;
  inProgressDeficiencies: number;
  awaitingEvidence: number;
  verificationPending: number;
  verifiedDeficiencies: number;
  highRiskDeficiencies: number;
  criticalRiskDeficiencies: number;
  slaOnTrack: number;
  slaWarning: number;
  slaAtRisk: number;
  slaBreached: number;
  escalatedDeficiencies: number;
  criticalEscalations: number;
  openCapas?: number;
  overdueCapas?: number;
  effectiveCapas?: number;
  ineffectiveCapas?: number;
  recurringIssues?: number;
  pendingEffectivenessReviews?: number;
  averageOpenCapaAgeDays?: number;
  agingBuckets?: {
    within30: number;
    days31To60: number;
    days61To90: number;
    over90: number;
  };
  departmentOverdueCapas?: DeficiencyCountByLabel[];
  byRootCauseCategory: DeficiencyCountByLabel[];
  byRiskLevel: DeficiencyCountByLabel[];
  byLifecycleStatus: DeficiencyCountByLabel[];
  byDepartment: DeficiencyCountByLabel[];
  byStage: DeficiencyCountByLabel[];
}

export interface DeficiencyWorkspaceItem {
  id: string;
  deficiencyCode: string;
  capaNumber?: string | null;
  ownerAcceptedAt?: string | null;
  ownerAcceptedBy?: string | null;
  ownerAcceptanceRejected?: boolean;
  title: string;
  description?: string | null;
  deficiencyType: string;
  severity: DeficiencySeverity | string;
  status: DeficiencyStatus | string;
  lifecycleStatus: DeficiencyLifecycleStatus | string;
  maturityLevel: string;
  rootCauseCategory?: RootCauseCategory | string | null;
  classification: string;

  objectiveElementId: string;
  objectiveElementCode: string;
  objectiveElementName: string;
  chapterCode: string;
  chapterName: string;
  standardCode: string;
  standardName: string;

  primaryRemediationTaskId?: string | null;
  primaryTaskTitle?: string | null;
  primaryTaskStatus?: string | null;
  assignedOwner?: string | null;
  department?: string | null;
  dueDate?: string | null;
  assignedDate?: string | null;
  targetResolutionDate?: string | null;
  actualResolutionDate?: string | null;
  isOverdue: boolean;

  slaStatus?: string | null;
  slaDueDate?: string | null;
  daysRemaining?: number | null;
  slaPercentConsumed?: number | null;
  escalationLevel?: number | null;
  escalationState?: string | null;

  correctiveActionStatus?: string | null;
  preventiveActionStatus?: string | null;
  capaOverdue?: boolean;
  isRecurringIssue?: boolean;
  effectivenessReviewStatus?: EffectivenessReviewStatus | string | null;

  riskLikelihood?: number | null;
  riskImpact?: number | null;
  riskScore?: number | null;
  assessedRiskLevel?: DeficiencyRiskLevel | string | null;

  evidenceStatus: string;
  verificationStatus: string;
  evidenceCount: number;
  rejectedEvidenceCount: number;
  needsMoreEvidenceCount: number;

  blocksCore: boolean;
  blocksCommitment: boolean;
  blocksAchievement: boolean;
  blocksExcellence: boolean;
  blockingStageLabel: string;

  detectedDate: string;
  updatedAt: string;
  closedDate?: string | null;
}

export interface DeficiencyTimelineEntry {
  action: string;
  actorType: string;
  occurredAt: string;
  detailJson?: string | null;
}

export interface DeficiencyWorkspaceDetail extends DeficiencyWorkspaceItem {
  riskCategory?: string | null;
  source: string;
  blockingReason?: string | null;
  lastEvaluatedDate: string;
  complianceObligationId: string;
  latestEvidenceTitle?: string | null;
  latestEvidenceDate?: string | null;
  originType?: string | null;
  originFindingId?: string | null;
  originFindingNumber?: string | null;
  originFindingSource?: string | null;
  promotedAt?: string | null;

  problemStatement?: string | null;
  rootCauseSummary?: string | null;
  impactDescription?: string | null;
  correctiveActionSummary?: string | null;
  preventiveActionSummary?: string | null;
  why1?: string | null;
  why2?: string | null;
  why3?: string | null;
  why4?: string | null;
  why5?: string | null;
  finalRootCause?: string | null;

  actionPlan?: string | null;
  expectedOutcome?: string | null;
  remediationTargetCompletion?: string | null;
  successCriteria?: string | null;

  capaLifecycleSteps?: CapaLifecycleStep[];
  correctiveActionOwner?: string | null;
  correctiveActionStartDate?: string | null;
  correctiveActionTargetDate?: string | null;
  correctiveActionCompletedDate?: string | null;
  correctiveActionOverdue?: boolean;
  preventiveActionOwner?: string | null;
  preventiveActionTargetDate?: string | null;
  preventiveActionCompletedDate?: string | null;
  preventiveActionOverdue?: boolean;
  preventiveActionSuccessCriteria?: string | null;
  effectivenessReviewScheduledDate?: string | null;
  effectivenessReviewDaysOffset?: number | null;
  effectivenessReviewDate?: string | null;
  effectivenessReviewer?: string | null;
  effectivenessReviewNotes?: string | null;
  effectivenessReviewOutcome?: string | null;
  recurrenceReason?: string | null;
  recurrenceDetectedAt?: string | null;

  timeline: DeficiencyTimelineEntry[];

  slaPolicyId?: string | null;
  slaPolicyName?: string | null;
  slaStartDate?: string | null;
  elapsedHours?: number | null;
  remainingHours?: number | null;
  overdueHours?: number | null;
  escalationTargetUserId?: string | null;
  escalationTargetLabel?: string | null;
  escalationHistory?: DeficiencyEscalationHistoryItem[];
}

export interface DeficiencyEscalationHistoryItem {
  id: string;
  level: number;
  targetUserId?: string | null;
  targetLabel: string;
  reason: string;
  state: string;
  triggeredAt: string;
  acknowledgedAt?: string | null;
  resolvedAt?: string | null;
}

export interface DeficiencyWorkspaceList {
  items: DeficiencyWorkspaceItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface DeficiencyWorkspaceQuery {
  status?: string;
  severity?: string;
  maturityLevel?: string;
  search?: string;
  objectiveElementId?: string;
  sortBy?: string;
  sortDesc?: boolean;
  page?: number;
  pageSize?: number;
}

export interface PatchDeficiencyRcaRequest {
  rootCauseCategory?: string;
  problemStatement?: string;
  rootCauseSummary?: string;
  impactDescription?: string;
  correctiveActionSummary?: string;
  preventiveActionSummary?: string;
  why1?: string;
  why2?: string;
  why3?: string;
  why4?: string;
  why5?: string;
  finalRootCause?: string;
}

export interface PatchDeficiencyRemediationRequest {
  riskLikelihood?: number;
  riskImpact?: number;
  department?: string;
  targetResolutionDate?: string;
  actionPlan?: string;
  expectedOutcome?: string;
  remediationTargetCompletion?: string;
  successCriteria?: string;
}

export interface PatchDeficiencyCorrectiveActionRequest {
  summary?: string;
  owner?: string;
  startDate?: string;
  targetDate?: string;
  completedDate?: string;
  status?: CapaActionStatus | string;
}

export interface PatchDeficiencyPreventiveActionRequest {
  summary?: string;
  owner?: string;
  targetDate?: string;
  completedDate?: string;
  successCriteria?: string;
  status?: CapaActionStatus | string;
}

export interface PatchDeficiencyEffectivenessReviewRequest {
  status?: EffectivenessReviewStatus | string;
  scheduledDate?: string;
  reviewDaysOffset?: number;
  reviewDate?: string;
  reviewer?: string;
  notes?: string;
  outcome?: string;
}

export interface DeficiencyCapaModel {
  deficiencyId: string;
  lifecycleSteps: CapaLifecycleStep[];
  isRecurringIssue: boolean;
  recurrenceReason?: string | null;
}

export type AssignmentConfidence = 'None' | 'Low' | 'Medium' | 'High';

export interface AssignmentRoutingSuggestion {
  recommendedAssigneeUserId?: string | null;
  recommendedDepartmentId?: string | null;
  recommendedDepartmentName?: string | null;
  normalizedDepartmentCode?: string | null;
  ruleApplied: number;
  ruleAppliedLabel: string;
  routingReason: string;
  confidence: AssignmentConfidence | string;
  canAutoAssign: boolean;
}

export interface AssignmentRoutingApplyResult {
  suggestion: AssignmentRoutingSuggestion;
  wasApplied: boolean;
  wasAutoAssigned: boolean;
  primaryRemediationTaskId?: string | null;
}
