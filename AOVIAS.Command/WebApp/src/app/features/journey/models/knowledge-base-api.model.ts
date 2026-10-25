/**
 * Gateway Knowledge Base API DTOs (`/api/v{version}/knowledge-base/…`).
 * @see ComplaincePlatform/docs/backend/gateway-knowledge-base-api-specification.md
 * @see docs/backend/knowledge-base-wizard-ui-binding.md (OE wizard read-only field matrix)
 * Responses are unwrapped to `data` by `apiResponseInterceptor` (same as Journey APIs).
 */

export interface ApiError {
  code: string;
  message: string;
  status?: number;
  details?: unknown;
}

/** Envelope shape when interceptor is bypassed (tests / resilience). */
export interface ApiResponse<T> {
  success: boolean;
  code: string;
  message: string;
  data: T | null;
  errors: ApiError[];
  meta?: {
    requestId?: string;
    timestamp: string;
  };
}

export interface FrameworkDto {
  id: string;
  code: string;
  name: string;
  version: string;
  description: string;
  isActive: boolean;
}

export interface ComplianceNodeDto {
  id: string;
  frameworkId: string;
  parentId: string | null;
  level: number;
  code: string;
  title: string;
  description: string;
  nodeType: string;
  sequence: number;
}

export interface KnowledgeItemDto {
  id: string;
  nodeId: string;
  explanation: string;
  whyItMatters: string;
  howToComply: string;
  evidenceRequired: string;
  auditChecklist: string;
  riskLevel: string;
  impactLevel: string;
  tags: string;
  version: number;
  isActive: boolean;
}

export interface RequirementDetailDto {
  id: string;
  nodeId: string;
  intent: string;
  measurableElements: string;
  classification: string;
  isMandatory: boolean;
  applicableDepartments: string;
  requiresDocumentation: boolean;
}

export interface RequirementScoreDto {
  id: string;
  nodeId: string;
  maxScore: number;
  weight: number;
  classificationId: string;
}

export interface NodeDetailsDto {
  node: ComplianceNodeDto | null;
  requirement: RequirementDetailDto | null;
  knowledge: KnowledgeItemDto[];
  requirementScore: RequirementScoreDto | null;
}

export interface AiAskRequestDto {
  userId: string;
  frameworkId: string;
  nodeId: string;
  query: string;
}

export interface AiAskResponseDto {
  interactionId: string;
  response: string;
  modelUsed: string;
  tokensUsed: number;
}
