/**
 * Sprint 13D – Approval Card Framework
 * Frontend models for CopilotActionProposal lifecycle.
 */

// ── Enums ─────────────────────────────────────────────────────────────────────

export enum ProposalStatus {
  Proposed = 0,
  Approved = 1,
  Rejected = 2,
  Expired  = 3,
  Executed = 4,
}

export enum ProposalType {
  CreateTask                  = 0,
  AssignTask                  = 1,
  CreateCapa                  = 2,
  ScheduleEffectivenessReview = 3,
  OpenEvidenceChecklist       = 4,
  GenerateExecutiveBriefing   = 5,
  CreateFinding               = 6,
  CreateDeficiency            = 7,
  CreateReadinessPlan         = 8,
  CreateRemediationPlan       = 9,
}

// ── Interfaces ────────────────────────────────────────────────────────────────

export interface CopilotActionProposal {
  id:               string;
  organizationId:   string;
  recommendationId: string;
  draftId?:         string;

  proposalType:     string;
  proposalTypeEnum: ProposalType;
  title:            string;
  description:      string;
  payloadJson:      string;

  proposedBy:    string;
  proposedAtUtc: string;
  expiresAtUtc:  string;

  status:         string;
  statusEnum:     ProposalStatus;
  approvedBy?:    string;
  approvedAtUtc?: string;
  rejectedBy?:    string;
  rejectedAtUtc?: string;
  rejectionReason?: string;

  resultEntityId?:   string;
  resultEntityType?: string;
  executionNotes?:   string;
  executedAtUtc?:    string;
  executedBy?:       string;

  isExpired:  boolean;
  canApprove: boolean;
  canReject:  boolean;
  canExecute: boolean;
}

export interface ProposalAuditEvent {
  id:            string;
  proposalId:    string;
  eventType:     string;
  actorId?:      string;
  actorEmail?:   string;
  notes?:        string;
  contextJson?:  string;
  occurredAtUtc: string;
}

// ── Request DTOs ──────────────────────────────────────────────────────────────

export interface CreateProposalRequest {
  recommendationId: string;
  draftId?:         string;
  proposalType:     ProposalType;
  title:            string;
  description:      string;
  payloadJson?:     string;
  ttlDays?:         number;
}

export interface GenerateProposalFromRecommendationRequest {
  proposalTypeOverride?: ProposalType;
  titleOverride?:        string;
  additionalContext?:    string;
  assignedUserId?:       string;
  dueDate?:              string;
}

export interface GenerateProposalFromDraftRequest {
  proposalTypeOverride?: ProposalType;
  titleOverride?:        string;
  additionalContext?:    string;
}

export interface ApproveProposalRequest {
  notes?: string;
}

export interface RejectProposalRequest {
  reason: string;
}

// ── Helpers ───────────────────────────────────────────────────────────────────

export function getProposalStatusLabel(status: ProposalStatus): string {
  switch (status) {
    case ProposalStatus.Proposed: return 'Pending Approval';
    case ProposalStatus.Approved: return 'Approved';
    case ProposalStatus.Rejected: return 'Rejected';
    case ProposalStatus.Expired:  return 'Expired';
    case ProposalStatus.Executed: return 'Executed';
    default:                      return 'Unknown';
  }
}

export function getProposalStatusColor(status: ProposalStatus): string {
  switch (status) {
    case ProposalStatus.Proposed: return 'warning';
    case ProposalStatus.Approved: return 'success';
    case ProposalStatus.Rejected: return 'danger';
    case ProposalStatus.Expired:  return 'secondary';
    case ProposalStatus.Executed: return 'primary';
    default:                      return 'secondary';
  }
}

export function getProposalTypeLabel(type: ProposalType): string {
  switch (type) {
    case ProposalType.CreateTask:                  return 'Create Compliance Task';
    case ProposalType.AssignTask:                  return 'Assign Task';
    case ProposalType.CreateCapa:                  return 'Create CAPA';
    case ProposalType.ScheduleEffectivenessReview: return 'Schedule Effectiveness Review';
    case ProposalType.OpenEvidenceChecklist:        return 'Open Evidence Checklist';
    case ProposalType.GenerateExecutiveBriefing:    return 'Generate Executive Briefing';
    case ProposalType.CreateFinding:               return 'Create Audit Finding';
    case ProposalType.CreateDeficiency:            return 'Create Deficiency';
    case ProposalType.CreateReadinessPlan:         return 'Create Readiness Plan';
    case ProposalType.CreateRemediationPlan:       return 'Create Remediation Plan';
    default:                                        return 'Proposed Action';
  }
}

export function getProposalTypeIcon(type: ProposalType): string {
  switch (type) {
    case ProposalType.CreateTask:
    case ProposalType.AssignTask:                  return 'check_circle';
    case ProposalType.CreateCapa:                  return 'assignment_turned_in';
    case ProposalType.ScheduleEffectivenessReview: return 'schedule';
    case ProposalType.OpenEvidenceChecklist:        return 'checklist';
    case ProposalType.GenerateExecutiveBriefing:    return 'summarize';
    case ProposalType.CreateFinding:               return 'report_problem';
    case ProposalType.CreateDeficiency:            return 'warning';
    case ProposalType.CreateReadinessPlan:
    case ProposalType.CreateRemediationPlan:       return 'article';
    default:                                        return 'auto_fix_high';
  }
}
