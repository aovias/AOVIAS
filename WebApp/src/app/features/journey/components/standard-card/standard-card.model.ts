import {
  ExecutionOeItemVm,
  ExecutionOeStatusTone,
  JourneyWorkflowStatus,
} from '../../models/execution-screen-vm.model';
import {
  ExecutionOeRowVm,
  ExecutionStandardVm,
  ExecutionStatus,
} from '../../models/journey-execution-vm.model';

/** Row model for OE table inside a standard card (presentation contract). */
export interface StandardCardOeModel {
  id: string;
  code: string;
  title: string;
  classification: string;
  scoreLabel: string;
  statusLabel: string;
  statusTone: ExecutionOeStatusTone;
  evidenceCountLabel: string;
  departmentLabel?: string | null;
  isSelected?: boolean;
  journeyStatus: JourneyWorkflowStatus;
  journeyStatusLabel: string;
  changedSinceLastAudit?: boolean;
  excellenceNote?: string | null;
  deficiencyCount?: number;
  openTaskCount?: number;
  evidenceStatusLabel?: string;
  verificationStatusLabel?: string;
  obligationStatusLabel?: string;
  readinessPercent?: number | null;
}

/** Standard card data — use mappers from legacy VM shapes when needed. */
export interface StandardModel {
  id: string;
  code: string;
  title: string;
  /** Full description below the title (optional). */
  description?: string;
  compliancePercent: number;
  classification: string;
  openIssuesCount?: number;
  objectiveElements: StandardCardOeModel[];
  complianceStatusLabel?: string | null;
  evidenceGapLabel?: string | null;
  changeTrend?: 'up' | 'down' | 'flat';
  previouslyFailed?: boolean;
  excellenceBadge?: boolean;
}

export type StandardCardActionKind =
  | 'view'
  | 'assign_action'
  | 'create_issue'
  | 'start_journey'
  | 'resume_journey';

export interface StandardCardActionEvent {
  action: StandardCardActionKind;
  standardId: string;
  oeId: string;
}

function mapExecutionStatusToTone(status: ExecutionStatus): ExecutionOeStatusTone {
  switch (status) {
    case 'compliant':
      return 'compliant';
    case 'non-compliant':
      return 'non-compliant';
    case 'partially-compliant':
      return 'partial';
    default:
      return 'na';
  }
}

function executionStatusLabel(status: ExecutionStatus): string {
  switch (status) {
    case 'compliant':
      return 'Compliant';
    case 'non-compliant':
      return 'Non-compliant';
    case 'partially-compliant':
      return 'Partial';
    default:
      return String(status);
  }
}

const DEFAULT_JOURNEY: JourneyWorkflowStatus = 'not_started';

export function mapExecutionOeRowVm(row: ExecutionOeRowVm): StandardCardOeModel {
  const statusTone = mapExecutionStatusToTone(row.status);
  return {
    id: row.id,
    code: row.code,
    title: row.title,
    classification: row.classification,
    scoreLabel: row.scoreLabel,
    statusLabel: executionStatusLabel(row.status),
    statusTone,
    evidenceCountLabel: String(row.evidenceCount),
    departmentLabel: null,
    isSelected: false,
    journeyStatus: DEFAULT_JOURNEY,
    journeyStatusLabel: 'Not Started',
  };
}

export function standardModelFromExecutionStandardVm(s: ExecutionStandardVm): StandardModel {
  return {
    id: s.id,
    code: s.code,
    title: s.title,
    description: s.description,
    compliancePercent: s.compliancePercent,
    classification: s.classification,
    openIssuesCount: s.openIssues,
    objectiveElements: s.objectiveElements.map(mapExecutionOeRowVm),
  };
}

export function standardModelFromExecutionStandardItemVm(s: {
  id: string;
  code: string;
  title: string;
  compliancePercent: number;
  classification: string;
  objectiveElements: ExecutionOeItemVm[];
  description?: string;
  openIssuesCount?: number;
  complianceStatusLabel?: string | null;
  evidenceGapLabel?: string | null;
  changeTrend?: 'up' | 'down' | 'flat';
  previouslyFailed?: boolean;
  excellenceBadge?: boolean;
}): StandardModel {
  return {
    id: s.id,
    code: s.code,
    title: s.title,
    description: s.description,
    compliancePercent: s.compliancePercent,
    classification: s.classification,
    openIssuesCount: s.openIssuesCount,
    complianceStatusLabel: s.complianceStatusLabel,
    evidenceGapLabel: s.evidenceGapLabel,
    changeTrend: s.changeTrend,
    previouslyFailed: s.previouslyFailed,
    excellenceBadge: s.excellenceBadge,
    objectiveElements: s.objectiveElements.map((oe) => ({
      id: oe.id,
      code: oe.code,
      title: oe.title,
      classification: oe.classification,
      scoreLabel: oe.scoreLabel,
      statusLabel: oe.statusLabel,
      statusTone: oe.statusTone,
      evidenceCountLabel: oe.evidenceCountLabel,
      departmentLabel: oe.departmentLabel,
      isSelected: oe.isSelected,
      journeyStatus: oe.journeyStatus,
      journeyStatusLabel: oe.journeyStatusLabel,
      changedSinceLastAudit: oe.changedSinceLastAudit,
      excellenceNote: oe.excellenceNote,
      deficiencyCount: oe.deficiencyCount,
      openTaskCount: oe.openTaskCount,
      evidenceStatusLabel: oe.evidenceStatusLabel,
      verificationStatusLabel: oe.verificationStatusLabel,
      obligationStatusLabel: oe.obligationStatusLabel,
      readinessPercent: oe.readinessPercent,
    })),
  };
}
