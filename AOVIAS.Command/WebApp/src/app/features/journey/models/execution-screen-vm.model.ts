import type { JourneyWorkflowStatus } from '../utils/journey-oe-status.util';

export type { JourneyWorkflowStatus };

export type ExecutionRiskTone = 'low' | 'medium' | 'high';

export interface ExecutionChapterNavItemVm {
  id: string;
  code: string;
  name: string;
  risk: ExecutionRiskTone;
  /** 0–100 for progress bar fill */
  compliancePercent: number;
  progressLabel: string;
  openIssuesCount: number;
  issueBadgeLabel: string;
  isActive: boolean;
  /** Final Audit — mock PASS/FAIL until API. */
  passFailLabel?: string | null;
  /** Surveillance — mock “changed since last audit”. */
  changedSinceLastAudit?: boolean;
  /** Re-accreditation — mock maturity copy. */
  maturityLabel?: string | null;
  /** Internal Audit — emphasize high-risk chapter rows. */
  highlightHighRisk?: boolean;
}

export interface ExecutionChapterPanelVm {
  title: string;
  subtitle: string;
  items: ExecutionChapterNavItemVm[];
  searchPlaceholder: string;
  sortByRiskLabel: string;
}

/** Visual status for OE row chips (mission-control readability). */
export type ExecutionOeStatusTone =
  | 'compliant'
  | 'partial'
  | 'non-compliant'
  | 'not-applicable'
  | 'na';

export interface ExecutionOeItemVm {
  id: string;
  code: string;
  title: string;
  classification: string;
  scoreLabel: string;
  statusLabel: string;
  statusTone: ExecutionOeStatusTone;
  evidenceCountLabel: string;
  isNa: boolean;
  hasActionPlan: boolean;
  isSelected: boolean;
  /** Short department label for badge column (matches workspace filter taxonomy). */
  departmentLabel: string | null;
  journeyStatus: JourneyWorkflowStatus;
  journeyStatusLabel: string;
  /** Compact execution signals for OE rows (deficiency / task counts by OE id). */
  deficiencyCount?: number;
  openTaskCount?: number;
  evidenceStatusLabel?: string;
  verificationStatusLabel?: string;
  obligationStatusLabel?: string;
  readinessPercent?: number | null;
  /** Surveillance — mock changed/unchanged. */
  changedSinceLastAudit?: boolean;
  /** Re-accreditation — extra classification line. */
  excellenceNote?: string | null;
}

export interface ExecutionStandardItemVm {
  id: string;
  code: string;
  title: string;
  /** Optional full description under the title (standard card). */
  description?: string;
  compliancePercent: number;
  classification: string;
  oeCountLabel: string;
  isActive: boolean;
  objectiveElements: ExecutionOeItemVm[];
  /** Open issues line hidden in Self Assessment via policy. */
  openIssuesCount?: number;
  /** Final Audit — replaces open-issues line. */
  complianceStatusLabel?: string | null;
  /** Internal Audit — evidence gap hint (mock). */
  evidenceGapLabel?: string | null;
  /** Surveillance — mock trend. */
  changeTrend?: 'up' | 'down' | 'flat';
  /** Surveillance — previously failed (mock). */
  previouslyFailed?: boolean;
  /** Re-accreditation */
  excellenceBadge?: boolean;
}

export interface ExecutionStandardsHeroVm {
  compliancePercent: number;
  risk: ExecutionRiskTone;
  openIssuesCount: number;
  chapterName: string;
  chapterCode: string;
}

export interface ExecutionStandardsPanelVm {
  title: string;
  subtitle: string;
  hero: ExecutionStandardsHeroVm | null;
  items: ExecutionStandardItemVm[];
}

/** Right-hand Journey Context panel (selected OE). */
export interface JourneyContextPanelVm {
  empty: boolean;
  /** Workspace loading — right panel may show skeletons when empty. */
  contextLoading?: boolean;
  /** Lifecycle / audit mode locks interactions. */
  readOnly?: boolean;
  /** KPI strip for Re-accreditation (mock label). */
  kpiInsightLabel?: string | null;
  /** 0–100 for determinate progress bar (from OE score 1–5). */
  progressPercent?: number;
  code?: string;
  title?: string;
  departmentLabel?: string | null;
  assigneeDisplayName?: string | null;
  frequencyLabel?: string;
  scoreLabel?: string;
  statusLabel?: string;
  statusTone?: ExecutionOeStatusTone;
  evidenceCountLabel?: string;
  lastEvidenceUpdated?: string | null;
  journeyStatus?: JourneyWorkflowStatus;
  journeyStatusLabel?: string;
  /** Obligation compliance state for the selected OE (Phase 4). */
  obligationState?: import('../models/journey-api.model').ObligationStateApiDto | null;
}

/** OE compliance tone for presentation helpers (e.g. standards row). */
export type ExecutionOeComplianceTone =
  | 'compliant'
  | 'partial'
  | 'non-compliant'
  | 'not-applicable';
