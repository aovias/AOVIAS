import { JourneyChapter } from './chapter.model';
import { OperationalMode } from './nabh.enums';
import type { MaturityStageId, MaturityStageStateModel } from '../../../shared/models/maturity-stage.model';

// ── Obligation domain types (Phase 4) ────────────────────────────────────────

export interface ObligationState {
  obligationId: string;
  implementationStatus: string;
  assessmentStatus: string;
  journeyStatus: string;
  evidenceStatus: string;
  verificationStatus: string;
  readinessScore: number;
  lastAssessmentDate: string | null;
  lastVerificationDate: string | null;
  nextActionLabel: string;
  nextActionKey: string;
  isNextActionUrgent: boolean;
}

export interface ObligationBandSummary {
  total: number;
  verified: number;
  implemented: number;
  partial: number;
  deficient: number;
  notStarted: number;
  unknown: number;
  readinessScore: number;
}

export interface ObligationMaturityBreakdown {
  core: ObligationBandSummary;
  commitment: ObligationBandSummary;
  achievement: ObligationBandSummary;
  excellence: ObligationBandSummary;
}

/** Stage obligation summary derived from StageProgressDto enrichment. */
export interface StageObligationSummary {
  stage: string;
  total: number;
  verified: number;
  implemented: number;
  partial: number;
  deficient: number;
  readinessScore: number;
}

/** Eligibility rules for accreditation panel (thresholds applied in UI). */
export interface AccreditationEligibility {
  overallCompliancePercent: number;
  coreOeAverageScore: number;
  averageStandardScore: number;
  averageChapterScore: number;
  /** Human-readable violation messages */
  violations: string[];
}

/** Per-maturity-stage score — populated from StageBreakdown in JourneyOverviewMetricsDto. */
export interface JourneyStageProgress {
  stage: 'core' | 'commitment' | 'achievement' | 'excellence';
  label: string;
  /** 0–100 compliance score for OEs of this stage classification. */
  score: number;
  totalOeCount: number;
  scoredOeCount: number;
  // Obligation enrichment (Phase 4)
  totalObligations?: number;
  verifiedObligations?: number;
  implementedObligations?: number;
  partialObligations?: number;
  deficientObligations?: number;
  obligationReadinessScore?: number;
}

export interface JourneyOverviewMetrics {
  /** % of core OEs meeting minimum score (e.g. ≥4) */
  coreCompliancePercent: number;
  failedCoreCount: number;
  oesAtOrBelowThreeCount: number;
  openActionPlansCount: number;
  /** Per-stage scores (Core / Commitment / Achievement / Excellence). */
  stageBreakdown: JourneyStageProgress[];
  // Obligation enrichment (Phase 4)
  obligationBreakdown?: ObligationMaturityBreakdown | null;
  obligationReadinessScore?: number;
}

export interface JourneyTrendPoint {
  label: string;
  value: number;
}

/** NABH Hospital 6th Edition catalog metadata (full guidebook = 639 OEs / 100 standards). */
export interface JourneyCatalogMeta {
  standardEdition: string;
  effectiveFrom: string;
  totalStandardsExpected: number;
  totalOesExpected: number;
  /** True when mock/API does not contain full catalog. */
  isPartialCatalog: boolean;
}

/** From GET …/session `modeProgression` (API-driven mode locks). */
export interface JourneyModeProgression {
  allowedNextModes: number[];
  lockReasonByTargetMode: Record<string, string | null>;
}

export interface JourneyWorkspaceSnapshot {
  journeyId: string;
  frameworkCode: string;
  frameworkName: string;
  catalog?: JourneyCatalogMeta;
  chapters: JourneyChapter[];
  accreditation: AccreditationEligibility;
  overview: JourneyOverviewMetrics;
  /** Sparkline / mini-chart series */
  oeScoreTrend: JourneyTrendPoint[];
  chapterImprovementTrend: JourneyTrendPoint[];
  riskEvolutionTrend: JourneyTrendPoint[];
  /** When provided by GET /Journeys/workspace */
  lifecycleMode?: OperationalMode;
  /** Server-enforced read-only (e.g. lifecycle); optional override */
  readOnly?: boolean;
  /** Snapshot / approved-locked read-only from session API */
  snapshotReadOnly?: boolean;
  /** Dashboard aggregate (nullable in API). */
  accreditationReadinessPercent?: number | null;
  /** From session — allowed next operational modes (0–4). */
  modeProgression?: JourneyModeProgression;
  /** ISO timestamp for “Last activity” footer (mock + future API). */
  lastSyncedAt?: string;
  /** Authoritative stage unlock state from StageUnlockingService (Sprint 1 — H2). */
  maturityStages?: MaturityStageStateModel[];
  currentMaturityStage?: MaturityStageId | null;
}
