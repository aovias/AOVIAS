/**
 * HTTP DTOs for Deficiency Engine APIs (`/api/v1/deficiencies/…`).
 * Phase 5 — Compliance Deficiency Engine.
 */

export interface DeficiencyDto {
  id: string;
  organizationId: string;
  hospitalId: string;
  frameworkId: string;
  objectiveElementId: string;
  complianceObligationId: string;
  deficiencyCode: string;
  title: string;
  description?: string | null;
  deficiencyType: string;
  severity: string;
  status: string;
  maturityLevel: string;
  riskCategory?: string | null;
  assignedTo?: string | null;
  dueDate?: string | null;
  source: string;
  detectedDate: string;
  closedDate?: string | null;
  lastEvaluatedDate: string;
  createdAt: string;
  updatedAt: string;
}

export interface DeficiencySummaryApiDto {
  critical: number;
  major: number;
  moderate: number;
  minor: number;
  total: number;
  openCount: number;
}

export interface DeficiencyByStageApiDto {
  stage: string;
  label: string;
  critical: number;
  major: number;
  moderate: number;
  minor: number;
  total: number;
}

export interface DeficiencyListApiDto {
  items: DeficiencyDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}
