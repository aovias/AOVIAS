/**
 * HTTP DTOs for Journey APIs (`/journeys/{journeyId}/…`) — payload after ApiResponse envelope unwrap.
 * Shapes are defensive: optional fields mirror partial/missing API responses.
 * @see journey-module-api-specification.md (Compliance Platform)
 */

import type { OeWizardPersistedState } from './oe-execution-wizard.model';

export interface JourneyTrendPointApiDto {
  label?: string | null;
  value?: number | null;
}

export interface AccreditationEligibilityApiDto {
  overallCompliancePercent?: number | null;
  coreOeAverageScore?: number | null;
  averageStandardScore?: number | null;
  averageChapterScore?: number | null;
  violations?: string[] | null;
}

// ── Obligation state types (Phase 4) ────────────────────────────────────────

export interface ObligationStateApiDto {
  obligationId: string;
  implementationStatus: string;
  assessmentStatus: string;
  journeyStatus: string;
  evidenceStatus: string;
  verificationStatus: string;
  readinessScore: number;
  lastAssessmentDate: string | null;
  lastVerificationDate: string | null;
  lastSnapshotRefreshDate?: string | null;
  nextActionLabel: string;
  nextActionKey: string;
  isNextActionUrgent: boolean;
}

export interface ObligationBandSummaryApiDto {
  total: number;
  verified: number;
  implemented: number;
  partial: number;
  deficient: number;
  notStarted?: number;
  unknown?: number;
  readinessScore: number;
}

export interface ObligationMaturityBreakdownApiDto {
  core: ObligationBandSummaryApiDto;
  commitment: ObligationBandSummaryApiDto;
  achievement: ObligationBandSummaryApiDto;
  excellence: ObligationBandSummaryApiDto;
}

// ── Stage Progress ───────────────────────────────────────────────────────────

export interface StageProgressApiDto {
  /** Stage identifier — one of: core | commitment | achievement | excellence */
  stage?: string | null;
  label?: string | null;
  /** 0–100 compliance score for OEs of this stage classification. */
  score?: number | null;
  totalOeCount?: number | null;
  scoredOeCount?: number | null;
  // Obligation enrichment (Phase 4)
  totalObligations?: number | null;
  verifiedObligations?: number | null;
  implementedObligations?: number | null;
  partialObligations?: number | null;
  deficientObligations?: number | null;
  obligationReadinessScore?: number | null;
}

export interface JourneyOverviewMetricsApiDto {
  coreCompliancePercent?: number | null;
  failedCoreCount?: number | null;
  oesAtOrBelowThreeCount?: number | null;
  openActionPlansCount?: number | null;
  /** Per-stage scores (Core / Commitment / Achievement / Excellence). */
  stageBreakdown?: StageProgressApiDto[] | null;
  // Obligation enrichment (Phase 4)
  obligationBreakdown?: ObligationMaturityBreakdownApiDto | null;
  obligationReadinessScore?: number | null;
}

export interface JourneyDashboardAccreditationApiDto {
  overallCompliancePercent?: number | null;
  coreOeAverageScore?: number | null;
  averageStandardScore?: number | null;
  averageChapterScore?: number | null;
  violations?: string[] | null;
}

/** GET /journeys/{journeyId}/dashboard */
export interface JourneyDashboardDto {
  overview?: JourneyOverviewMetricsApiDto | null;
  accreditation?: JourneyDashboardAccreditationApiDto | null;
  accreditationReadinessPercent?: number | null;
  oeScoreTrend?: JourneyTrendPointApiDto[] | null;
  chapterImprovementTrend?: JourneyTrendPointApiDto[] | null;
  riskEvolutionTrend?: JourneyTrendPointApiDto[] | null;
  /** Authoritative stage from StageUnlockingService (Sprint 1 — H2). */
  currentStage?: string | null;
  /** Per-stage unlock state — same source as Compliance Maturity dashboard. */
  maturityStages?: import('../../../shared/models/maturity-stage.model').MaturityStageStateApiDto[] | null;
}

export interface JourneyCatalogMetaApiDto {
  standardEdition?: string | null;
  effectiveFrom?: string | null;
  totalStandardsExpected?: number | null;
  totalOesExpected?: number | null;
  isPartialCatalog?: boolean | null;
}

export interface JourneyModeProgressionApiDto {
  allowedNextModes?: number[] | null;
  lockReasonByTargetMode?: Record<string, string | null> | null;
}

/** GET /journeys/{journeyId}/session */
export interface JourneySessionDto {
  journeyId?: string | null;
  frameworkCode?: string | null;
  frameworkName?: string | null;
  operationalMode?: number | null;
  readOnly?: boolean | null;
  snapshotReadOnly?: boolean | null;
  lastSyncedAt?: string | null;
  catalog?: JourneyCatalogMetaApiDto | null;
  modeProgression?: JourneyModeProgressionApiDto | null;
}

export interface JourneyChapterIndexItemDto {
  id?: string | null;
  code?: string | null;
  name?: string | null;
  complianceScorePercent?: number | null;
  risk?: string | null;
  coreComplianceStatus?: string | null;
  openIssuesCount?: number | null;
  pendingAudit?: boolean | null;
  coreFailureFlag?: boolean | null;
  issueBadgeLabel?: string | null;
  passFailLabel?: string | null;
  changedSinceLastAudit?: boolean | null;
  maturityLabel?: string | null;
  highlightHighRisk?: boolean | null;
}

/** GET /journeys/{journeyId}/chapters — wrapped list */
export interface JourneyChaptersIndexDto {
  items?: JourneyChapterIndexItemDto[] | null;
}

export interface OeActionPlanApiDto {
  id?: string | null;
  title?: string | null;
  ownerName?: string | null;
  deadline?: string | null;
  status?: string | null;
  accepted?: boolean | null;
}

export interface OeEvidenceItemApiDto {
  id?: string | null;
  name?: string | null;
  /** Optional — technical filename with extension when `name` is display-only. */
  fileName?: string | null;
  /** Optional — Gateway may expose MIME when `name` is a display title without extension. */
  mimeType?: string | null;
  contentType?: string | null;
  /** Optional — URL path may end with `.png` etc. when `name` does not. */
  fileUrl?: string | null;
  verificationStatus?: string | null;
  uploadedAt?: string | null;
  expiryAt?: string | null;
  uploadedBy?: string | null;
}

export interface OeHistoryEventApiDto {
  id?: string | null;
  type?: string | null;
  title?: string | null;
  description?: string | null;
  timestamp?: string | null;
  actor?: string | null;
}

/** GET/PATCH OE detail (spec). */
export interface ObjectiveElementApiDto {
  id?: string | null;
  standardId?: string | null;
  code?: string | null;
  title?: string | null;
  description?: string | null;
  /** Integer band per backend (0–4 typical). */
  classification?: number | string | null;
  score?: number | null;
  scoreLabel?: string | null;
  scoreRationale?: string | null;
  evidenceStatus?: string | null;
  isNa?: boolean | null;
  naJustification?: string | null;
  auditStatus?: string | null;
  departmentLabel?: string | null;
  assigneeDisplayName?: string | null;
  lastUpdatedAt?: string | null;
  dueAt?: string | null;
  nextReviewAt?: string | null;
  auditRemarks?: string | null;
  actionPlans?: OeActionPlanApiDto[] | null;
  evidence?: OeEvidenceItemApiDto[] | null;
  history?: OeHistoryEventApiDto[] | null;
  journeyWorkflowStatus?: string | null;
  changedSinceLastAudit?: boolean | null;
  excellenceNote?: string | null;
  version?: number | null;
  isMandatory?: boolean | null;
  isScorable?: boolean | null;
  isApplicable?: boolean | null;
  isDocumentationRequired?: boolean | null;
  weight?: number | null;
  isCoreCompliant?: boolean | null;
  evidenceVerifiedFlag?: boolean | null;
  /** Obligation compliance state — present when obligation sync has run (Phase 4). */
  obligationState?: ObligationStateApiDto | null;
}

export interface ComplianceStandardApiDto {
  id?: string | null;
  chapterId?: string | null;
  code?: string | null;
  name?: string | null;
  title?: string | null;
  description?: string | null;
  sequence?: number | null;
  weight?: number | null;
  objectiveElements?: ObjectiveElementApiDto[] | null;
  averageScore?: number | null;
  coreOeAverage?: number | null;
  naCount?: number | null;
  completionPercent?: number | null;
  riskBadge?: string | null;
  coreSummary?: string | null;
  openIssuesCount?: number | null;
  complianceStatusLabel?: string | null;
  evidenceGapLabel?: string | null;
  changeTrend?: string | null;
  previouslyFailed?: boolean | null;
  excellenceBadge?: boolean | null;
}

export interface JourneyChapterApiDto {
  id?: string | null;
  code?: string | null;
  name?: string | null;
  complianceScorePercent?: number | null;
  risk?: string | null;
  coreComplianceStatus?: string | null;
  openIssuesCount?: number | null;
  pendingAudit?: boolean | null;
  coreFailureFlag?: boolean | null;
  standards?: ComplianceStandardApiDto[] | null;
}

/** Legacy workspace GET (optional shim). */
export interface JourneyWorkspaceApiDto {
  journeyId?: string | null;
  frameworkCode?: string | null;
  frameworkName?: string | null;
  chapters?: JourneyChapterApiDto[] | null;
  accreditation?: AccreditationEligibilityApiDto | null;
  overview?: JourneyOverviewMetricsApiDto | null;
  oeScoreTrend?: JourneyTrendPointApiDto[] | null;
  chapterImprovementTrend?: JourneyTrendPointApiDto[] | null;
  riskEvolutionTrend?: JourneyTrendPointApiDto[] | null;
  operationalMode?: number | string | null;
  auditLifecycleMode?: number | string | null;
  lifecycleMode?: number | string | null;
  readOnly?: boolean | null;
  lastSyncedAt?: string | null;
}

/** PATCH /objective-elements/{oeId} */
export interface PatchObjectiveElementBody {
  version?: number;
  isNa?: boolean;
  naJustification?: string | null;
  score?: number;
  scoreRationale?: string | null;
  auditRemarks?: string | null;
}

export interface PatchObjectiveElementResponseDto {
  objectiveElement?: ObjectiveElementApiDto | null;
  overview?: JourneyOverviewMetricsApiDto | null;
  version?: number | null;
}

export interface PatchOperationalModeRequestDto {
  operationalMode: number;
}

export interface RegisterOeEvidenceRequest {
  evidenceType: string;
  fileUrl: string;
  title?: string | null;
  metadata?: string | null;
}

/** POST evidence success body */
export interface RegisterOeEvidenceResponseDto {
  evidenceId?: string | null;
}

/** Gateway §5.5 — per-file row in bulk upload response. */
export type BulkEvidenceUploadItemStatus = 'Success' | 'Failed' | string;

export interface BulkEvidenceUploadItemDto {
  fileName?: string | null;
  status?: BulkEvidenceUploadItemStatus | null;
  evidenceId?: string | null;
  errorMessage?: string | null;
}

/** POST …/evidence/bulk — Gateway `BulkEvidenceUploadResponseDto` (§5.5). */
export interface BulkEvidenceUploadResponseDto {
  totalFiles?: number | null;
  successCount?: number | null;
  failureCount?: number | null;
  items?: BulkEvidenceUploadItemDto[] | null;
}

/** Gateway §5.8 — verify or reject OE-linked evidence. */
export interface VerifyOeEvidenceRequest {
  accepted: boolean;
  remarks?: string | null;
}

/** Gateway §5.9 — optional fields vary by backend; extend as UI needs. */
export interface EvidenceVersionApiDto {
  id?: string | null;
  fileUrl?: string | null;
  uploadedAt?: string | null;
  notes?: string | null;
}

/** Gateway §5.6 — evidence detail (subset for client typing). */
export interface EvidenceDetailDto {
  id?: string | null;
  title?: string | null;
  name?: string | null;
  fileName?: string | null;
  originalFileName?: string | null;
  evidenceType?: string | null;
  fileUrl?: string | null;
  mimeType?: string | null;
  contentType?: string | null;
  uploadedAt?: string | null;
  versions?: EvidenceVersionApiDto[] | null;
}

export interface DeleteOeEvidenceResponseDto {
  deleted?: boolean | null;
}

/** Mirrors Gateway / Compliance `ObjectiveJourneyStatusDto` JSON strings. */
export type ObjectiveJourneyStatusDto = 'draft' | 'inProgress' | 'completed';

/** Handles camelCase strings, PascalCase, or numeric enum wire values from proxies. */
export function isObjectiveJourneyStatusCompleted(
  status: ObjectiveJourneyStatusDto | string | number | null | undefined,
): boolean {
  if (status == null) return false;
  if (typeof status === 'number' && Number.isFinite(status)) {
    return Math.round(status) === 2;
  }
  const s = String(status).trim().toLowerCase();
  return s === 'completed' || s === '2';
}

/** Maps wizard DTO status to `ObjectiveElement.journeyWorkflowStatus` (Compliance workspace string). */
export function objectiveJourneyWizardStatusToJourneyWorkflowStatus(
  status: ObjectiveJourneyStatusDto | string | number | null | undefined,
): 'not_started' | 'in_progress' | 'completed' {
  if (status == null) {
    return 'not_started';
  }
  if (typeof status === 'number' && Number.isFinite(status)) {
    const n = Math.round(status);
    if (n === 2) {
      return 'completed';
    }
    if (n === 1) {
      return 'in_progress';
    }
    return 'not_started';
  }
  const s = String(status).trim().toLowerCase();
  if (s === 'completed' || s === '2') {
    return 'completed';
  }
  if (s === 'inprogress' || s === 'in_progress' || s === '1') {
    return 'in_progress';
  }
  return 'not_started';
}

/** GET/PUT/POST …/oe-execution-wizard — payload after ApiResponse unwrap. */
export interface ObjectiveJourneyStateDto {
  id?: string | null;
  journeyId: string;
  objectiveElementId: string;
  hospitalId: string;
  status: ObjectiveJourneyStatusDto;
  lastUpdatedAt: string;
  completedAt?: string | null;
  rowVersion: string;
  payload: OeWizardPersistedState;
}

export interface SaveObjectiveJourneyDraftRequest {
  payload: OeWizardPersistedState;
  rowVersion?: string | null;
}

export interface CompleteObjectiveJourneyRequest {
  payload: OeWizardPersistedState;
  rowVersion?: string | null;
  complianceTaskId?: string | null;
}
