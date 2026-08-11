import {
  ActionPlanStatus,
  AuditComplianceStatus,
  EvidenceStatus,
  NabhScore,
  OeClassification,
  OeHistoryEventType,
  ScoreLabel,
} from './nabh.enums';
import { ObligationStateApiDto } from './journey-api.model';

export interface OeActionPlan {
  id: string;
  title: string;
  ownerName: string;
  deadline: string;
  status: ActionPlanStatus;
  /** NABH: accepted corrective action plan with timelines (PDF). */
  accepted?: boolean;
}

export interface OeEvidenceItem {
  id: string;
  name: string;
  /** Original / storage filename when `name` is a display title without extension. */
  fileName?: string | null;
  /** When Gateway sends it (e.g. `image/png`), used for type label when `name` has no extension. */
  mimeType?: string | null;
  /** When Gateway sends it, basename may include `.png` even if `name` does not. */
  fileUrl?: string | null;
  verificationStatus: 'verified' | 'pending' | 'rejected';
  uploadedAt: string;
  expiryAt: string | null;
  uploadedBy: string;
}

export interface OeHistoryEvent {
  id: string;
  type: OeHistoryEventType;
  title: string;
  description: string;
  timestamp: string;
  actor: string;
}

/** Leaf unit: Objective Element (NABH hierarchy) — replaces legacy “clause” row. */
export interface ObjectiveElement {
  id: string;
  /** Parent standard id (NABH hierarchy). */
  standardId: string;
  code: string;
  /** Short title from API when distinct from description. */
  title?: string;
  description: string;
  classification: OeClassification;
  score: NabhScore;
  scoreLabel: ScoreLabel;
  scoreRationale?: string;
  evidenceStatus: EvidenceStatus;
  /** Server summary: ≥1 verified evidence link (list rows may omit `evidence[]`). */
  evidenceVerifiedFlag?: boolean;
  isNa: boolean;
  naJustification?: string;
  auditStatus: AuditComplianceStatus;
  departmentLabel?: string;
  assigneeDisplayName?: string;
  lastUpdatedAt?: string;
  dueAt?: string | null;
  nextReviewAt?: string | null;
  actionPlans: OeActionPlan[];
  evidence: OeEvidenceItem[];
  history: OeHistoryEvent[];
  auditRemarks?: string;
  /** Optimistic concurrency for PATCH OE. */
  version?: number;
  journeyWorkflowStatus?: string;
  changedSinceLastAudit?: boolean | null;
  excellenceNote?: string;
  /** Obligation compliance state — populated during Phase 4 enrichment. */
  obligationState?: ObligationStateApiDto | null;
}

/** At least one evidence row verified — required by backend when setting OE score to 5. */
export function oeHasVerifiedEvidence(oe: ObjectiveElement): boolean {
  if (oe.evidenceVerifiedFlag === true) return true;
  if (oe.evidenceStatus === 'complete') return true;
  return (oe.evidence ?? []).some((e) => e.verificationStatus === 'verified');
}

/** True when score ≤ 3 per product rules (action plan required). */
export function oeRequiresActionPlan(oe: ObjectiveElement): boolean {
  if (oe.isNa) return false;
  return oe.score <= 3;
}
