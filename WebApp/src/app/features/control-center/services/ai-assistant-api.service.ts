import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from '../../../shared/services/base-api.service';
import { ComplianceContextService } from '../../../core/services/compliance-context.service';
import { AiPageContextService } from '../../../core/services/ai-page-context.service';
import { CopilotStore } from '../../copilot/state/copilot.store';
import { PERSONA_DISPLAY_NAMES } from '../../copilot/models/copilot-persona.model';

export interface AiQueryContext {
  objectiveElementId?: string | null;
  taskId?: string | null;
  evidenceId?: string | null;
  department?: string | null;
  conversationSummary?: string | null;
  recentEntityRefs?: string[];
}

export interface AiReasoningStep {
  code: string;
  message: string;
}

export interface AiSourceReference {
  sourceType: string;
  sourceId: string;
  summary?: string | null;
  relevanceScore: number;
  /** Phase 5 — version-aware citation lineage */
  documentId?: string | null;
  version?: string | null;
  excerpt?: string | null;
}

export interface AiGroundedAnswerDto {
  answer: string;
  citations: AiSourceReference[];
  reasoning: AiReasoningStep[];
  confidenceScore: number;
  modelVersion?: string;
  traceId?: string;
  answerMode?: AiAnswerMode;
  humanApprovalRequired?: boolean;
}

export interface KnowledgeSearchQuery {
  query: string;
  hospitalId?: string;
  frameworkId?: string | null;
  mode?: 'keyword' | 'semantic' | 'hybrid';
  topK?: number;
}

export interface KnowledgeSearchResultDto {
  query: string;
  mode: string;
  citations: AiSourceReference[];
  total: number;
}

export interface CapaSuggestRequest {
  deficiencyId?: string | null;
  objectiveElementId?: string | null;
  deficiencySummary?: string | null;
  rootCauseHint?: string | null;
  useLlmAssist?: boolean;
}

export interface CapaSuggestionItemDto {
  kind: string;
  title: string;
  proposal: string;
  expectedOutcome: string;
  confidenceScore: number;
  citations?: AiSourceReference[];
}

export interface CapaSuggestResultDto {
  suggestions: CapaSuggestionItemDto[];
  summary: string;
  humanApprovalRequired: boolean;
  traceId?: string;
}

export type AiAnswerMode =
  | 'GeneralKnowledge'
  | 'ComplianceKnowledge'
  | 'OrganizationSpecific'
  | 'Hybrid'
  | 'SmallTalk';

/** Severity used for finding and risk badges (matches backend ComplianceSeverity). */
export type ComplianceSeverity = 'Critical' | 'High' | 'Medium' | 'Low';

/** Shape of the compliance question; lets the UI adapt section headers. */
export type ComplianceInsightType =
  | 'General'
  | 'ComplianceScore'
  | 'Risk'
  | 'Evidence'
  | 'Standard';

export interface ComplianceFindingDto {
  title: string;
  severity: ComplianceSeverity;
  impact: string;
  detail: string;
}

export interface ComplianceRiskDto {
  title: string;
  severity: ComplianceSeverity;
  affectedControls: string[];
}

export interface RecommendedActionDto {
  priority: number;
  action: string;
  expectedOutcome: string;
}

export interface SupportingEvidenceDto {
  label: string;
  sourceType: string;
}

export interface TechnicalMetadataDto {
  confidence: number;
  answerMode: string;
  contextVersionId: string;
  rawSources: AiSourceReference[];
}

/** Executive Compliance Advisor presentation model. Optional for backward compatibility. */
export interface CompliancePresentationDto {
  insightType: ComplianceInsightType;
  executiveSummary: string;
  findings: ComplianceFindingDto[];
  risks: ComplianceRiskDto[];
  recommendations: RecommendedActionDto[];
  expectedOutcome: string;
  supportingEvidence: SupportingEvidenceDto[];
  technical: TechnicalMetadataDto;
  findingCount: number;
  riskCount: number;
  actionCount: number;
  evidenceCount: number;
}

export interface AiResponseDto {
  complianceHint: string;
  confidenceScore: number;
  compositeConfidenceScore: number;
  reasoning: AiReasoningStep[];
  sourceReferences: AiSourceReference[];
  validationOutcome: 'accept' | 'correct' | 'reject';
  contextVersionId: string;
  answerMode?: AiAnswerMode;
  /** Present when the backend produced the executive Compliance Advisor presentation. */
  presentation?: CompliancePresentationDto | null;
  /** Phase 5 — grounded projection with citations + confidence. */
  grounded?: AiGroundedAnswerDto | null;
  humanApprovalRequired?: boolean;
}

@Injectable({ providedIn: 'root' })
export class AiAssistantApiService extends BaseApiService {
  constructor(
    private readonly complianceContext: ComplianceContextService,
    private readonly pageContext: AiPageContextService,
    private readonly copilotStore: CopilotStore,
  ) {
    super();
  }

  sendQuery(prompt: string, extra?: AiQueryContext): Observable<AiResponseDto> {
    const { frameworkId, hospitalId, organizationId } = this.requireCanonicalContext();
    const page = this.pageContext.snapshot();
    const persona = this.copilotStore.effectivePersona();
    const personaLabel = PERSONA_DISPLAY_NAMES[persona];
    const workspace = this.copilotStore.personaWorkspace();
    const personaContextParts = [
      `Active persona: ${personaLabel}`,
      workspace?.landing.focusArea ? `Focus: ${workspace.landing.focusArea}` : null,
      workspace?.definition.primaryObjectives?.length
        ? `Objectives: ${workspace.definition.primaryObjectives.join('; ')}`
        : null,
    ].filter(Boolean);
    const personaContext = personaContextParts.join(' · ');
    const conversationSummary = [
      personaContext,
      extra?.conversationSummary ?? null,
    ].filter((p) => p && p.trim().length > 0).join(' | ');
    return this.post<AiResponseDto>('/ai/query', {
      prompt,
      promptTemplateId: 'ExplainCompliance',
      promptTemplateVersion: '1.0',
      frameworkId,
      hospitalId,
      organizationId,
      pageContext: page.pageContext,
      entityType: page.entityType ?? (extra?.objectiveElementId ? 'ObjectiveElement' : null),
      entityId: page.entityId ?? extra?.objectiveElementId ?? null,
      objectiveElementId: extra?.objectiveElementId ?? null,
      taskId: extra?.taskId ?? null,
      evidenceId: extra?.evidenceId ?? null,
      department: extra?.department ?? null,
      conversationSummary: conversationSummary || null,
      recentEntityRefs: extra?.recentEntityRefs ?? this.pageContext.getRecentEntityRefs(),
    });
  }

  getAssistiveSuggestions(prompt: string, objectiveElementId?: string): Observable<AiResponseDto> {
    const { frameworkId, hospitalId } = this.requireCanonicalContext();
    return this.post<AiResponseDto>('/ai/assist', {
      prompt,
      promptTemplateId: 'SuggestMapping',
      promptTemplateVersion: '1.0',
      objectiveElementId: objectiveElementId ?? null,
      frameworkId,
      hospitalId,
    });
  }

  /** Phase 5.1 — hybrid/semantic/keyword knowledge search. */
  knowledgeSearch(query: string, options?: { mode?: 'keyword' | 'semantic' | 'hybrid'; topK?: number }): Observable<KnowledgeSearchResultDto> {
    const { frameworkId, hospitalId } = this.requireCanonicalContext();
    const body: KnowledgeSearchQuery = {
      query,
      hospitalId,
      frameworkId,
      mode: options?.mode ?? 'hybrid',
      topK: options?.topK ?? 8,
    };
    return this.post<KnowledgeSearchResultDto>('/ai/knowledge/search', body);
  }

  /** Phase 5.4 — CAPA corrective/preventive proposals (never auto-applied). */
  suggestCapa(request: CapaSuggestRequest): Observable<CapaSuggestResultDto> {
    const { frameworkId, hospitalId, organizationId } = this.requireCanonicalContext();
    return this.post<CapaSuggestResultDto>('/ai/advisor/capa-suggest', {
      hospitalId,
      frameworkId,
      organizationId: organizationId ?? null,
      deficiencyId: request.deficiencyId ?? null,
      objectiveElementId: request.objectiveElementId ?? null,
      deficiencySummary: request.deficiencySummary ?? null,
      rootCauseHint: request.rootCauseHint ?? null,
      useLlmAssist: request.useLlmAssist ?? false,
    });
  }

  private requireCanonicalContext(): { frameworkId: string; hospitalId: string; organizationId?: string | null } {
    const frameworkId = this.complianceContext.getFrameworkId();
    const hospitalId = this.complianceContext.getHospitalId();
    const organizationId = this.complianceContext.getOrganizationId();
    if (!frameworkId || !hospitalId) {
      throw new Error('AI assistant requires canonical frameworkId and hospitalId in compliance context.');
    }

    return { frameworkId, hospitalId, organizationId };
  }
}
