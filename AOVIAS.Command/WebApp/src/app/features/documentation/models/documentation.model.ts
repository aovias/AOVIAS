import type {
  ControlledDocumentStatus,
  ControlledDocumentType,
  DocumentAcknowledgementAssigneeType,
  DocumentApprovalStatus,
} from './documentation.enums';

export interface DocumentationDashboard {
  totalDocuments: number;
  published: number;
  draft: number;
  underReview: number;
  expired: number;
  dueForReview: number;
  pendingApprovals: number;
  pendingAcknowledgements: number;
  departmentCoverage: number;
  oeCoverage: number;
  recentDocuments: ControlledDocumentListItem[];
}

export interface ControlledDocumentListItem {
  id: string;
  documentNumber: string;
  title: string;
  documentType: ControlledDocumentType;
  department?: string | null;
  ownerName?: string | null;
  reviewerName?: string | null;
  approverName?: string | null;
  currentVersion?: string | null;
  currentVersionId?: string | null;
  status: ControlledDocumentStatus;
  effectiveDate?: string | null;
  reviewDate?: string | null;
  expiryDate?: string | null;
  mappedFramework?: string | null;
  mappedChapter?: string | null;
  mappedStandard?: string | null;
  mappedObjectiveElement?: string | null;
  createdAt: string;
  lastUpdatedAt: string;
}

export interface ControlledDocumentDetail extends ControlledDocumentListItem {
  hospitalId?: string | null;
  nextReviewDate?: string | null;
  reviewFrequencyMonths: number;
  reviewOwnerName?: string | null;
  mappedObjectiveElementId?: string | null;
  content?: string | null;
  versions: DocumentVersionSummary[];
  approvals: DocumentApproval[];
  auditTrail: DocumentLifecycleAudit[];
  acknowledgementStats: DocumentAcknowledgementStats;
}

export interface DocumentVersionSummary {
  id: string;
  versionNumber: number;
  versionLabel: string;
  changeSummary?: string | null;
  isCurrent: boolean;
  isSuperseded: boolean;
  createdByName?: string | null;
  createdAt: string;
}

export interface DocumentApproval {
  id: string;
  approvalOrder: number;
  approverRole: string;
  approverName?: string | null;
  status: DocumentApprovalStatus;
  decidedAt?: string | null;
  comments?: string | null;
}

export interface DocumentLifecycleAudit {
  id: string;
  eventType: string;
  fromStatus?: ControlledDocumentStatus | null;
  toStatus?: ControlledDocumentStatus | null;
  performedByName?: string | null;
  details?: string | null;
  createdAt: string;
}

export interface DocumentAcknowledgementStats {
  total: number;
  read: number;
  acknowledged: number;
  pending: number;
  completionPercent: number;
}

export interface DocumentAcknowledgementListItem {
  id: string;
  documentNumber: string;
  title: string;
  assigneeName: string;
  assigneeType: DocumentAcknowledgementAssigneeType;
  readAt?: string | null;
  acknowledgedAt?: string | null;
  reminderCount: number;
}

export interface ReviewCalendarItem {
  documentId: string;
  documentNumber: string;
  title: string;
  nextReviewDate?: string | null;
  effectiveDate?: string | null;
  reviewOwnerName?: string | null;
  reviewFrequencyMonths: number;
  status: ControlledDocumentStatus;
  reminderTier: string;
}

export interface DocumentTemplate {
  id: string;
  name: string;
  category: string;
  documentType: ControlledDocumentType;
  description?: string | null;
  isBuiltIn: boolean;
  contentTemplate: string;
}

export interface DocumentationAnalytics {
  policiesDueReview: number;
  expiredDocuments: number;
  acknowledgementPercent: number;
  departmentComplianceCount: number;
  mostRevisedSops: MostRevisedDocument[];
  unmappedDocuments: number;
  reviewCyclePerformancePercent: number;
  approvalTurnaroundDays: number;
  frameworkCoveragePercent: number;
}

export interface MostRevisedDocument {
  id: string;
  documentNumber: string;
  title: string;
  revisionCount: number;
}

export interface CreateControlledDocumentRequest {
  title: string;
  documentType: ControlledDocumentType;
  department?: string;
  ownerName?: string;
  content?: string;
  templateId?: string;
  mappedFramework?: string;
  mappedChapter?: string;
  mappedStandard?: string;
  mappedObjectiveElementId?: string;
  mappedObjectiveElement?: string;
}

export interface CreateDocumentVersionRequest {
  content: string;
  changeSummary?: string;
}

export interface TransitionDocumentStatusRequest {
  targetStatus: ControlledDocumentStatus;
  comments?: string;
}

export interface ApproveDocumentRequest {
  documentVersionId: string;
  approved?: boolean;
  comments?: string;
}

export interface PublishDocumentRequest {
  effectiveDate?: string;
  expiryDate?: string;
  requireReAcknowledge?: boolean;
}

export interface AcknowledgeDocumentRequest {
  acknowledgementId: string;
  markReadOnly?: boolean;
}

export interface CompleteDocumentReviewRequest {
  outcome?: string;
  notes?: string;
  noChangesRequired?: boolean;
  nextReviewDate?: string;
}

export interface DocumentsQuery {
  type?: ControlledDocumentType;
  status?: ControlledDocumentStatus;
  search?: string;
}
