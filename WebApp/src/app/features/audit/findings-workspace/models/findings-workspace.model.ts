/** Sprint 7A / 7A.1 — Findings Workbench API models */

export type FindingClassification =
  | 'AuditFinding'
  | 'Observation'
  | 'NonConformity'
  | 'OpportunityForImprovement'
  | 'GoodPractice';

export type PromotionReadiness = 'Not Eligible' | 'Awaiting Acceptance' | 'Eligible' | 'Promoted';

export type FindingSource =
  | 'InternalAudit'
  | 'ExternalAudit'
  | 'ComplianceTeam'
  | 'ConsultantAssessment'
  | 'NabhAssessment'
  | 'ManagementReview'
  | 'DepartmentAssessment'
  | 'IncidentReview';

export type FindingSeverity = 'Critical' | 'Major' | 'Moderate' | 'Minor' | 'Informational';

export type FindingLifecycleStatus =
  | 'Draft'
  | 'Open'
  | 'Reviewed'
  | 'Accepted'
  | 'Closed'
  | 'InProgress'
  | 'Verified';

export interface FindingsCountByLabel {
  label: string;
  count: number;
}

export interface FindingsWorkspaceDashboard {
  totalFindings: number;
  openFindings: number;
  acceptedFindings: number;
  closedFindings: number;
  criticalFindings: number;
  draftFindings: number;
  awaitingReview: number;
  eligibleForPromotion: number;
  promotedFindings: number;
  findingsAwaitingPromotion: number;
  promotionRate: number;
  bySource: FindingsCountByLabel[];
  byDepartment: FindingsCountByLabel[];
  byChapter: FindingsCountByLabel[];
  byClassification: FindingsCountByLabel[];
}

export interface FindingsWorkspaceItem {
  id: string;
  findingNumber: string;
  title: string;
  description?: string | null;
  classification: FindingClassification | string;
  source: FindingSource | string;
  severity: FindingSeverity | string;
  status: FindingLifecycleStatus | string;
  department: string;
  owner?: string | null;
  chapterCode: string;
  chapterName: string;
  standardCode: string;
  standardName: string;
  objectiveElementCode: string;
  objectiveElementName: string;
  objectiveElementId?: string | null;
  clauseReference: string;
  createdDate: string;
  lastUpdated: string;
  attachmentCount: number;
  commentCount: number;
  isPromoted: boolean;
  promotionReadiness: PromotionReadiness | string;
}

export interface FindingComment {
  id: string;
  authorUserId: string;
  body: string;
  createdAt: string;
}

export interface FindingAttachment {
  id: string;
  fileName: string;
  contentType?: string | null;
  fileUrl: string;
  attachmentType: string;
  fileSizeBytes?: number | null;
  uploadedAt: string;
}

export interface FindingHistoryEntry {
  action: string;
  actorUserId?: string | null;
  actorType: string;
  occurredAt: string;
  detail?: string | null;
}

export interface FindingLifecycleStep {
  key: string;
  label: string;
  isComplete: boolean;
  isCurrent: boolean;
  occurredAt?: string | null;
  actorUserId?: string | null;
}

export interface FindingPromotionEligibility {
  findingId: string;
  eligibilityStatus: string;
  isEligible: boolean;
  validationReasons: string[];
}

/** Sprint 7B — promotion eligibility and readiness from GET /findings/{id}/promotion-status */
export interface FindingPromotionStatus {
  findingId: string;
  eligibilityStatus: string;
  isEligible: boolean;
  isReady: boolean;
  isPromoted: boolean;
  allowPromotion: boolean;
  promotedDeficiencyId?: string | null;
  primaryRemediationTaskId?: string | null;
  findingNumber?: string | null;
  validationMessages: string[];
}

export interface PromoteFindingResult {
  findingId: string;
  deficiencyId: string;
  deficiencyCode: string;
  primaryRemediationTaskId?: string | null;
  promotedAt: string;
}

export interface FindingsWorkspaceDetail extends FindingsWorkspaceItem {
  comments: FindingComment[];
  attachments: FindingAttachment[];
  history: FindingHistoryEntry[];
  lifecycleSteps: FindingLifecycleStep[];
  promotedDeficiencyId?: string | null;
  auditDate?: string | null;
  auditorName?: string | null;
  auditCycle?: string | null;
  observation?: string | null;
  requirement?: string | null;
  impact?: string | null;
  recommendation?: string | null;
  dateRaised: string;
}

export interface FindingComplianceContextOption {
  objectiveElementId: string;
  objectiveElementCode: string;
  objectiveElementName: string;
  standardCode: string;
  standardName: string;
  chapterCode: string;
  chapterName: string;
}

export interface CreateFindingAttachmentInput {
  fileName: string;
  fileUrl: string;
  contentType?: string;
  attachmentType: string;
  fileSizeBytes?: number;
}

export interface CreateStructuredFindingRequest {
  title: string;
  description?: string;
  clauseReference: string;
  severity: string;
  department: string;
  raisedBy: string;
  classification: string;
  status: string;
  dateRaised?: string;
  objectiveElementId?: string;
  /** DEF-M6-001 — links the finding to the audit session it was raised in. */
  auditSessionId?: string;
  auditDate?: string;
  auditorName?: string;
  auditCycle?: string;
  observation?: string;
  requirement?: string;
  impact?: string;
  recommendation?: string;
  attachments?: CreateFindingAttachmentInput[];
}

export interface FindingsWorkspaceList {
  items: FindingsWorkspaceItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface FindingsWorkspaceQuery {
  status?: string;
  severity?: string;
  classification?: string;
  source?: string;
  department?: string;
  promotionReadiness?: string;
  chapterCode?: string;
  standardCode?: string;
  objectiveElementId?: string;
  quickFilter?: string;
  search?: string;
  sortBy?: string;
  sortDesc?: boolean;
  page?: number;
  pageSize?: number;
}
