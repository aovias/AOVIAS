import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from '../../../shared/services/base-api.service';
import type { CopilotRecommendation } from '../models/copilot-recommendation.model';
import type { AiDraft, GenerateDraftRequest } from '../models/ai-draft.model';
import type { PersonaInfo } from '../models/copilot-persona.model';
import type {
  CopilotActionProposal,
  ProposalAuditEvent,
  CreateProposalRequest,
  GenerateProposalFromRecommendationRequest,
  GenerateProposalFromDraftRequest,
  ApproveProposalRequest,
  RejectProposalRequest,
} from '../models/proposal.model';
import type { ProposalStatus, ProposalType } from '../models/proposal.model';

@Injectable({ providedIn: 'root' })
export class CopilotApiService extends BaseApiService {
  /** `apiBaseUrl` already includes `/api/v1` — do not duplicate the prefix. */
  private readonly base = '/copilot';

  // ── Recommendations ───────────────────────────────────────────────────────

  getRecommendations(): Observable<CopilotRecommendation[]> {
    return this.get<CopilotRecommendation[]>(
      `${this.base}/recommendations`,
    );
  }

  getRecommendationsByPersona(persona: string, max = 5): Observable<CopilotRecommendation[]> {
    return this.get<CopilotRecommendation[]>(
      `${this.base}/recommendations/persona/${persona}`,
      { max: max.toString() },
    );
  }

  getRecommendation(id: string): Observable<CopilotRecommendation> {
    return this.get<CopilotRecommendation>(
      `${this.base}/recommendations/${id}`,
    );
  }

  refreshRecommendations(request?: {
    frameworkId?: string;
    persona?: string;
  }): Observable<CopilotRecommendation[]> {
    return this.post<CopilotRecommendation[]>(
      `${this.base}/recommendations/refresh`,
      request ?? {},
    );
  }

  // ── Drafts ────────────────────────────────────────────────────────────────

  getDrafts(): Observable<AiDraft[]> {
    return this.get<AiDraft[]>(
      `${this.base}/drafts`,
    );
  }

  getDraft(id: string): Observable<AiDraft> {
    return this.get<AiDraft>(
      `${this.base}/drafts/${id}`,
    );
  }

  /** Returns all versions of the draft group for a given draft ID. Sprint 13C. */
  getDraftVersions(id: string): Observable<AiDraft[]> {
    return this.get<AiDraft[]>(
      `${this.base}/drafts/${id}/versions`,
    );
  }

  /** Generates a new draft from a generation request. Sprint 13C. */
  generateDraft(request: GenerateDraftRequest): Observable<AiDraft> {
    return this.post<AiDraft>(
      `${this.base}/drafts/generate`,
      request,
    );
  }

  /** Regenerates a new version of an existing draft. Sprint 13C. */
  regenerateDraft(id: string): Observable<AiDraft> {
    return this.post<AiDraft>(
      `${this.base}/drafts/${id}/regenerate`,
      {},
    );
  }

  // ── Sprint 13D: Proposals ─────────────────────────────────────────────────

  getProposals(params?: {
    status?: ProposalStatus;
    proposalType?: ProposalType;
    recommendationId?: string;
    page?: number;
    pageSize?: number;
  }): Observable<CopilotActionProposal[]> {
    return this.get<CopilotActionProposal[]>(
      `${this.base}/proposals`,
      params as Record<string, any>,
    );
  }

  getPendingProposals(): Observable<CopilotActionProposal[]> {
    return this.get<CopilotActionProposal[]>(
      `${this.base}/proposals/pending`,
    );
  }

  getProposal(id: string): Observable<CopilotActionProposal> {
    return this.get<CopilotActionProposal>(
      `${this.base}/proposals/${id}`,
    );
  }

  getProposalAuditTrail(id: string): Observable<ProposalAuditEvent[]> {
    return this.get<ProposalAuditEvent[]>(
      `${this.base}/proposals/${id}/audit-trail`,
    );
  }

  createProposal(request: CreateProposalRequest): Observable<CopilotActionProposal> {
    return this.post<CopilotActionProposal>(
      `${this.base}/proposals`,
      request,
    );
  }

  generateProposalFromRecommendation(
    recommendationId: string,
    request?: GenerateProposalFromRecommendationRequest,
  ): Observable<CopilotActionProposal> {
    return this.post<CopilotActionProposal>(
      `${this.base}/proposals/from-recommendation/${recommendationId}`,
      request ?? {},
    );
  }

  generateProposalFromDraft(
    draftId: string,
    request?: GenerateProposalFromDraftRequest,
  ): Observable<CopilotActionProposal> {
    return this.post<CopilotActionProposal>(
      `${this.base}/proposals/from-draft/${draftId}`,
      request ?? {},
    );
  }

  approveProposal(id: string, request?: ApproveProposalRequest): Observable<CopilotActionProposal> {
    return this.post<CopilotActionProposal>(
      `${this.base}/proposals/${id}/approve`,
      request ?? {},
    );
  }

  rejectProposal(id: string, request: RejectProposalRequest): Observable<CopilotActionProposal> {
    return this.post<CopilotActionProposal>(
      `${this.base}/proposals/${id}/reject`,
      request,
    );
  }

  executeProposal(id: string): Observable<{ proposalId: string; success: boolean; resultEntityId?: string; resultEntityType?: string; message?: string }> {
    return this.post<{ proposalId: string; success: boolean; resultEntityId?: string; resultEntityType?: string; message?: string }>(
      `${this.base}/proposals/${id}/execute`,
      {},
    );
  }

  // ── Personas ──────────────────────────────────────────────────────────────

  getPersonas(): Observable<PersonaInfo[]> {
    return this.get<PersonaInfo[]>(
      `${this.base}/personas`,
    );
  }

  getActivePersona(params?: {
    pageContext?: string;
    focusedEntityType?: string;
    focusedEntityId?: string;
    personaOverride?: string;
    conversationSummary?: string;
  }): Observable<PersonaInfo> {
    return this.get<PersonaInfo>(
      `${this.base}/personas/active`,
      params as Record<string, any>,
    );
  }

  // ── Sprint 13E: Persona Experience ──────────────────────────────────────────

  getExperienceCatalog(): Observable<import('../models/persona-experience.model').PersonaExperienceDefinition[]> {
    return this.get<import('../models/persona-experience.model').PersonaExperienceDefinition[]>(
      `${this.base}/experience/catalog`,
    );
  }

  getPersonaWorkspace(persona: string, frameworkId?: string): Observable<import('../models/persona-experience.model').PersonaWorkspace> {
    return this.get<import('../models/persona-experience.model').PersonaWorkspace>(
      `${this.base}/experience/workspace`,
      { persona, ...(frameworkId ? { frameworkId } : {}) },
    );
  }

  getActivityFeed(limit = 50): Observable<import('../models/persona-experience.model').CopilotActivityEvent[]> {
    return this.get<import('../models/persona-experience.model').CopilotActivityEvent[]>(
      `${this.base}/activity`,
      { limit: limit.toString() },
    );
  }

  getUsageMetrics(): Observable<import('../models/persona-experience.model').CopilotUsageMetrics> {
    return this.get<import('../models/persona-experience.model').CopilotUsageMetrics>(
      `${this.base}/usage-metrics`,
    );
  }

  recordActivity(
    request: import('../models/persona-experience.model').RecordCopilotActivityRequest,
  ): Observable<{ recorded: boolean }> {
    return this.post<{ recorded: boolean }>(
      `${this.base}/activity`,
      request,
    );
  }

  // ── Sprint 13F: Observability Analytics ───────────────────────────────────

  getAnalyticsOverview(): Observable<import('../models/copilot-analytics.model').CopilotGovernanceDashboard> {
    return this.get<import('../models/copilot-analytics.model').CopilotGovernanceDashboard>(
      `${this.base}/analytics/overview`,
    );
  }

  getAnalyticsRecommendations(): Observable<import('../models/copilot-analytics.model').RecommendationEffectivenessMetrics> {
    return this.get<import('../models/copilot-analytics.model').RecommendationEffectivenessMetrics>(
      `${this.base}/analytics/recommendations`,
    );
  }

  getAnalyticsDrafts(): Observable<import('../models/copilot-analytics.model').DraftEffectivenessMetrics> {
    return this.get<import('../models/copilot-analytics.model').DraftEffectivenessMetrics>(
      `${this.base}/analytics/drafts`,
    );
  }

  getAnalyticsProposals(): Observable<import('../models/copilot-analytics.model').ProposalConversionMetrics> {
    return this.get<import('../models/copilot-analytics.model').ProposalConversionMetrics>(
      `${this.base}/analytics/proposals`,
    );
  }

  getAnalyticsPersonas(): Observable<import('../models/copilot-analytics.model').PersonaAdoptionMetrics> {
    return this.get<import('../models/copilot-analytics.model').PersonaAdoptionMetrics>(
      `${this.base}/analytics/personas`,
    );
  }

  getAnalyticsTrends(days = 30): Observable<import('../models/copilot-analytics.model').CopilotTrends> {
    return this.get<import('../models/copilot-analytics.model').CopilotTrends>(
      `${this.base}/analytics/trends`,
      { days: days.toString() },
    );
  }

  getAnalyticsImpact(limit = 20): Observable<import('../models/copilot-analytics.model').RecommendationImpactSummary> {
    return this.get<import('../models/copilot-analytics.model').RecommendationImpactSummary>(
      `${this.base}/analytics/impact`,
      { limit: limit.toString() },
    );
  }
}
