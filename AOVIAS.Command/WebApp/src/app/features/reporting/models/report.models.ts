export interface ChapterCompliance {
  chapterId: string;
  chapterCode: string;
  chapterName: string;
  score: number;
  status: string;
}

export interface TrendPoint {
  date: string;
  value: number;
}

export interface RiskChapter {
  chapterId: string;
  chapterCode: string;
  chapterName: string;
  score: number;
  severity: string;
  details: string;
}

export interface ComplianceReport {
  totalClauses: number;
  compliantClauses: number;
  partialClauses: number;
  nonCompliantClauses: number;
  complianceScore: number;
  chapters: ChapterCompliance[];
  trends: TrendPoint[];
  topRiskChapters: RiskChapter[];
  generatedAt?: string;
}

export interface ChapterFinding {
  chapterId: string;
  chapterCode: string;
  chapterName: string;
  critical: number;
  major: number;
  minor: number;
  total: number;
}

export interface DepartmentFinding {
  department: string;
  count: number;
}

export interface AuditReport {
  criticalFindings: number;
  majorFindings: number;
  minorFindings: number;
  totalFindings: number;
  findingsByChapter: ChapterFinding[];
  findingsByDepartment: DepartmentFinding[];
  generatedAt?: string;
}

export interface DepartmentTask {
  departmentName: string;
  count: number;
}

export interface TaskReport {
  completionRate: number;
  overdueTasks: number;
  completedTasks: number;
  inProgressTasks: number;
  departmentTasks: DepartmentTask[];
  generatedAt?: string;
}

export interface ChapterEvidence {
  chapterId: string;
  chapterCode: string;
  chapterName: string;
  approved: number;
  pending: number;
  total: number;
}

export interface EvidenceReport {
  approvedEvidence: number;
  pendingReview: number;
  rejectedEvidence: number;
  coverageRatio: number;
  chapterEvidence: ChapterEvidence[];
  generatedAt?: string;
}

export interface HighRiskChapter {
  chapterId: string;
  chapterCode: string;
  chapterName: string;
  level: string;
  details: string;
}

export interface MissingEvidence {
  objectiveElementId: string;
  clauseCode: string;
  description: string;
}

export interface Recommendation {
  text: string;
  priority: string;
}

export interface ReadinessReport {
  readinessScore: number;
  highRiskChapters: HighRiskChapter[];
  missingEvidenceClauses: MissingEvidence[];
  recommendations: Recommendation[];
  generatedAt?: string;
}

export interface ColumnMetadata {
  key: string;
  label: string;
  type: string;
}

export interface CustomReportRequest {
  dimensions: string[];
  frameworkId?: string;
  chapterId?: string;
  departmentId?: string;
  severity?: string;
  fromDate?: string;
  toDate?: string;
}

export interface CustomReportResult {
  columns: ColumnMetadata[];
  rows: Record<string, unknown>[];
  generatedAt?: string;
}

export interface ReportQueryParams {
  frameworkId?: string;
  chapterId?: string;
  departmentId?: string;
  severity?: string;
  status?: string;
  fromDate?: string;
  toDate?: string;
  organizationId?: string;
}

export type ReportExportFormat = 'pdf' | 'excel' | 'csv';
export type ReportType = 'compliance' | 'audit' | 'tasks' | 'evidence' | 'readiness' | 'custom';
