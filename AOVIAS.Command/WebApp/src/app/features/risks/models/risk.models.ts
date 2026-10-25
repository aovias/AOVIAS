export type RiskStatus = 'Identified' | 'Assessed' | 'Mitigating' | 'UnderReview' | 'Closed';

export type RiskSeverity = 'Critical' | 'Major' | 'Minor' | 'Informational';

export interface RiskSummary {
  id: string;
  title: string;
  category?: string | null;
  status: string;
  severity: string;
  likelihood: number;
  impact: number;
  riskScore: number;
  residualLikelihood?: number | null;
  residualImpact?: number | null;
  residualRiskScore?: number | null;
  ownerUserId?: string | null;
  ownerName?: string | null;
  departmentId?: string | null;
  reviewDueDate?: string | null;
  identifiedAt: string;
  lastUpdatedAt: string;
}

export interface RiskDetail extends RiskSummary {
  tenantId: string;
  hospitalId: string;
  description?: string | null;
  mitigationPlan?: string | null;
  objectiveElementId?: string | null;
  taskId?: string | null;
  findingId?: string | null;
  incidentId?: string | null;
  deficiencyId?: string | null;
  closedAt?: string | null;
}

export interface RiskList {
  items: RiskSummary[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface RiskQuery {
  status?: string;
  category?: string;
  search?: string;
  ownerUserId?: string;
  departmentId?: string;
  reviewDueOnly?: boolean;
  page?: number;
  pageSize?: number;
}

export interface CreateRiskRequest {
  title: string;
  description?: string;
  category?: string;
  severity?: RiskSeverity | number;
  likelihood: number;
  impact: number;
  residualLikelihood?: number;
  residualImpact?: number;
  ownerUserId?: string;
  ownerName?: string;
  mitigationPlan?: string;
  reviewDueDate?: string;
  departmentId?: string;
  findingId?: string;
  incidentId?: string;
  deficiencyId?: string;
  status?: RiskStatus;
}

export interface UpdateRiskRequest extends Partial<CreateRiskRequest> {}

export interface RiskMatrixCell {
  likelihood: number;
  impact: number;
  score: number;
  count: number;
  band: string;
  risks: { id: string; title: string; status: string; riskScore: number }[];
}

export interface RiskMatrix {
  cells: RiskMatrixCell[];
  totalActiveRisks: number;
  highScoreCount: number;
  reviewDueCount: number;
}
