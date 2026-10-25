export interface EvidenceBinderFrameworkContext {
  frameworkId: string;
}

export interface EvidenceBinderSummary {
  totalObjectives: number;
  objectivesWithEvidence: number;
  objectivesMissingEvidence: number;
  objectivesApproved: number;
  approvedEvidenceCount: number;
  readinessPercent: number;
}

export interface EvidenceBinderChapter {
  chapterId: string;
  code: string;
  name: string;
  objectiveCount: number;
  objectivesWithEvidence: number;
  coveragePercent: number;
  riskLevel: 'low' | 'medium' | 'high';
}

export interface EvidenceBinderObjectiveElement {
  objectiveElementId: string;
  chapterId: string;
  code: string;
  title: string;
  description: string;
  evidenceCount: number;
  taskCount: number;
  status: 'missing' | 'partial' | 'approved';
}

export interface EvidenceBinderOverview {
  objectiveElementId: string;
  chapterId: string;
  chapterCode: string;
  chapterName: string;
  code: string;
  title: string;
  description: string;
  evidenceCount: number;
  taskCount: number;
  status: 'missing' | 'partial' | 'approved';
}

import type { EvidenceVerificationStatus } from '../services/evidence-api.service';

export interface EvidenceBinderEvidenceItem {
  id: string;
  evidenceId: string;
  fileName: string;
  fileUrl: string;
  fileType: string;
  fileSizeBytes: number;
  uploadedBy: string;
  uploadedAt: string;
  status: EvidenceVerificationStatus;
  isApproved?: boolean | null;
  reviewComment?: string | null;
  rowVersion?: string | null;
  verifiedBy?: string | null;
  verifiedAt?: string | null;
  version: number;
}

export interface EvidenceBinderChecklistItem {
  id: string;
  objectiveElementId: string;
  item: string;
  isCompleted: boolean;
}

/** AI mapping suggestions — always require human approval before linking. */
export interface EvidenceBinderAiMappingItem {
  id: string;
  title: string;
  detail?: string | null;
  confidencePercent: number;
  reasoning?: string | null;
  sourceReference?: string | null;
  humanApprovalRequired?: boolean;
}

export interface OeHistoryEvent {
  id: string;
  type: string;
  title: string;
  description?: string | null;
  timestamp: string;
  actor?: string | null;
}

export interface PagedEvidenceBinderEvidences {
  items: EvidenceBinderEvidenceItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export type BinderTab = 'evidence' | 'checklist' | 'ai-mapping' | 'history';

export type BinderDrawerMode = 'side' | 'fullscreen';
