import type { TaskApiStatus, TaskRiskLevelApi, TaskCriticalityLevel, ComplianceTaskPriority } from './task-risk.util';

export interface FrameworkContext {
  frameworkId: string;
  hospitalId: string | null;
}

export interface TaskRelatedDeficiency {
  id: string;
  title: string;
  deficiencyType: string;
  severity: string;
  status: string;
  description?: string | null;
}

export interface TaskRelatedObligation {
  id: string;
  evidenceStatus: string;
  verificationStatus: string;
  assessmentStatus: string;
  implementationStatus: string;
  readinessScore: number;
  nextActionLabel?: string | null;
}

export interface TaskSummary {
  total: number;
  notStarted: number;
  inProgress: number;
  blocked: number;
  completed: number;
  overdue: number;
}

export interface TaskListItem {
  id: string;
  title: string | null;
  description: string | null;
  objectiveElementId: string;
  objectiveElementCode: string;
  chapterId: string;
  assignedToUserId: string;
  assignedToDisplayName: string | null;
  departmentId: string | null;
  departmentName: string | null;
  priority: ComplianceTaskPriority;
  status: TaskApiStatus;
  riskLevel: TaskRiskLevelApi;
  riskScore: number;
  isOverdue: boolean;
  dueDate: string | null;
  createdAt: string;
  rowVersion: string;
  criticality: TaskCriticalityLevel;
  /** Merged evidence totals from list projection */
  evidenceTotalCount: number;
  evidenceApprovedCount: number;
  /** Merged evidences awaiting review (pending OE verification or no link yet). */
  evidencePendingReviewCount: number;
  sourceDeficiencyId?: string | null;
  relatedDeficiency?: TaskRelatedDeficiency | null;
  relatedObligation?: TaskRelatedObligation | null;
  maturityStageLabel?: string | null;
  objectiveElementTitle?: string | null;
  category?: TaskCategory;
  taskKind?: TaskKind;
  blockedByTaskId?: string | null;
  isBlockedByDependency?: boolean;
}

export type TaskCategory =
  | 'Compliance'
  | 'CAPA'
  | 'Audit'
  | 'Training'
  | 'DocumentReview'
  | 'General';

export type TaskKind = 'Standard' | 'Approval' | 'Review';

export interface TaskDetail extends TaskListItem {
  complianceStandardId: string;
  standardCode: string;
  maturityState: string | null;
}

export interface TaskKanbanResponse {
  notStarted: TaskListItem[];
  inProgress: TaskListItem[];
  blocked: TaskListItem[];
  completed: TaskListItem[];
  truncated: boolean;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface TaskEvidenceItem {
  evidenceId: string;
  fileName: string;
  fileUrl: string;
  fileType: string;
  fileSizeBytes: number;
  uploadedBy: string;
  uploadedAt: string;
  status: string;
  isApproved: boolean | null;
  reviewComment: string | null;
  source: 'objectiveElement' | 'task' | 'both';
  version: number;
}

export interface TaskComment {
  id: string;
  taskId: string;
  authorUserId: string;
  body: string;
  createdAt: string;
}

export interface TaskHistoryEntry {
  id: string;
  action: string;
  performedBy: string;
  timestamp: string;
  metadataJson: string;
}

/** Toolbar chips: map to API via TaskApiService.filterParams */
export type TaskQuickFilter =
  | 'myTasks'
  | 'team'
  | 'overdue'
  | 'highPriority'
  | 'dueThisWeek'
  | 'highRisk'
  | 'focus'
  | 'criticalDeficiencies'
  | 'evidenceMissing'
  | 'verificationRejected'
  | 'coreStage'
  | 'commitmentStage';

export type DeficiencySeverityFilter = 'Critical' | 'Major' | 'Moderate' | 'Minor';
export type DeficiencyTypeFilter =
  | 'Evidence'
  | 'Verification'
  | 'Implementation'
  | 'Assessment'
  | 'Maturity';
export type MaturityStageFilter = 'Core' | 'Commitment' | 'Achievement' | 'Excellence';
export type ObligationEvidenceStatusFilter = 'Missing' | 'Partial' | 'Submitted' | 'Verified';
export type ObligationVerificationStatusFilter = 'NotVerified' | 'Pending' | 'Verified' | 'Rejected';

export type TaskDrawerTab = 'details' | 'evidence' | 'comments' | 'history';

export interface TaskFilters {
  status?: TaskApiStatus;
  priority?: number;
  ownerId?: string;
  departmentId?: string;
  chapterId?: string;
  riskLevel?: TaskRiskLevelApi;
  search?: string;
  quick?: TaskQuickFilter;
  /** Critical risk band + overdue (server); sent as query param `focus` when true */
  focus?: boolean;
  deficiencySeverity?: DeficiencySeverityFilter;
  deficiencyType?: DeficiencyTypeFilter;
  maturityStage?: MaturityStageFilter;
  obligationEvidenceStatus?: ObligationEvidenceStatusFilter;
  obligationVerificationStatus?: ObligationVerificationStatusFilter;
  deficiencyLinkedOnly?: boolean;
  category?: TaskCategory;
  taskKind?: TaskKind;
  /** Inclusive ISO date (yyyy-MM-dd) for calendar window */
  dueFrom?: string;
  dueTo?: string;
}

/** Strip leading evidence id / GUID prefix from stored file names for display. */
export function displayEvidenceFileName(fileName: string): string {
  const t = fileName.trim();
  const noHash = t.replace(/^[a-f0-9]{32}_/i, '');
  if (noHash !== t) return noHash || t;
  const noGuid = t.replace(/^[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}_/i, '');
  return noGuid || t;
}

/** Map numeric priority from API (enum int) to label for risk util if needed */
export function priorityNumberToLabel(p: number): ComplianceTaskPriority {
  switch (p) {
    case 1:
      return 'Urgent';
    case 2:
      return 'High';
    case 3:
      return 'Medium';
    case 4:
    default:
      return 'Low';
  }
}

export function criticalityNumberToLabel(c: number): TaskCriticalityLevel {
  switch (c) {
    case 1:
      return 'Critical';
    case 2:
      return 'High';
    case 3:
      return 'Medium';
    case 4:
    default:
      return 'Low';
  }
}
