export type ExecutionRiskLevel = 'low' | 'medium' | 'high';
export type ExecutionSortMode = 'risk' | 'code';
export type ExecutionStatus = 'compliant' | 'partially-compliant' | 'non-compliant';

export interface ExecutionChapterItemVm {
  id: string;
  code: string;
  title: string;
  risk: ExecutionRiskLevel;
  objectiveCount: number;
  progressPercent: number;
}

export interface ExecutionOeRowVm {
  id: string;
  code: string;
  title: string;
  classification: string;
  scoreLabel: string;
  status: ExecutionStatus;
  evidenceCount: number;
}

export interface ExecutionStandardVm {
  id: string;
  code: string;
  title: string;
  /** Optional full description shown under the title in standard cards. */
  description?: string;
  compliancePercent: number;
  classification: string;
  openIssues: number;
  objectiveElements: ExecutionOeRowVm[];
}
