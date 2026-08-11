import { Injectable, inject, signal, computed } from '@angular/core';
import { catchError, EMPTY } from 'rxjs';
import { CopilotApiService } from '../services/copilot-api.service';
import { CopilotPersona, PERSONA_DISPLAY_NAMES } from '../models/copilot-persona.model';
import type { PersonaInfo } from '../models/copilot-persona.model';
import type { CopilotRecommendation } from '../models/copilot-recommendation.model';
import { RecommendationStatus, RecommendationSeverity, SEVERITY_ORDER } from '../models/copilot-recommendation.model';
import type { AiDraft, GenerateDraftRequest } from '../models/ai-draft.model';
import { DraftType, DraftSourceType, DraftStatus, recommendationCategoryToDraftType } from '../models/ai-draft.model';
import type { CopilotContext } from '../models/copilot-context.model';
import type {
  CopilotActionProposal,
  GenerateProposalFromRecommendationRequest,
  ApproveProposalRequest,
  RejectProposalRequest,
} from '../models/proposal.model';
import { ProposalStatus } from '../models/proposal.model';
import {
  PERSONA_OVERRIDE_SESSION_KEY,
  COPILOT_ACTIVITY,
} from '../models/persona-experience.model';
import type {
  PersonaExperienceDefinition,
  PersonaWorkspace,
  CopilotActivityEvent,
  CopilotUsageMetrics,
} from '../models/persona-experience.model';
import type { CopilotTrends } from '../models/copilot-analytics.model';
import { ComplianceContextService } from '../../../core/services/compliance-context.service';

interface CopilotLoadingState {
  recommendations: boolean;
  drafts: boolean;
  persona: boolean;
  catalog: boolean;
  refresh: boolean;
  generating: boolean;
  versions: boolean;
  proposals: boolean;
  approving: boolean;
  executing: boolean;
  workspace: boolean;
  activity: boolean;
  metrics: boolean;
  trends: boolean;
}

@Injectable({ providedIn: 'root' })
export class CopilotStore {
  private readonly api = inject(CopilotApiService);
  private readonly complianceContext = inject(ComplianceContextService);

  // ── State ─────────────────────────────────────────────────────────────────

  readonly activePersona = signal<CopilotPersona>(CopilotPersona.AccreditationCoach);
  readonly personaOverride = signal<CopilotPersona | null>(null);
  readonly personaCatalog = signal<PersonaInfo[]>([]);

  readonly recommendations = signal<CopilotRecommendation[]>([]);
  readonly drafts = signal<AiDraft[]>([]);

  /** The currently selected/active draft in the viewer. Sprint 13C. */
  readonly activeDraft = signal<AiDraft | null>(null);

  /** Version history for the currently selected draft group. Sprint 13C. */
  readonly draftVersions = signal<AiDraft[]>([]);

  readonly copilotContext = signal<CopilotContext | null>(null);

  // ── Sprint 13D: Proposals ─────────────────────────────────────────────────
  readonly proposals         = signal<CopilotActionProposal[]>([]);
  readonly activeProposal    = signal<CopilotActionProposal | null>(null);
  readonly proposalAuditTrail = signal<import('../models/proposal.model').ProposalAuditEvent[]>([]);

  // ── Sprint 13E: Persona Experience ────────────────────────────────────────
  readonly experienceCatalog = signal<PersonaExperienceDefinition[]>([]);
  readonly personaWorkspace = signal<PersonaWorkspace | null>(null);
  readonly activityFeed = signal<CopilotActivityEvent[]>([]);
  readonly usageMetrics = signal<CopilotUsageMetrics | null>(null);
  readonly analyticsTrends = signal<CopilotTrends | null>(null);
  readonly activeWorkspaceTab = signal<'overview' | 'recommendations' | 'drafts' | 'approvals' | 'history' | 'analytics'>('overview');

  readonly loading = signal<CopilotLoadingState>({
    recommendations: false,
    drafts:          false,
    persona:         false,
    catalog:         false,
    refresh:         false,
    generating:      false,
    versions:        false,
    proposals:       false,
    approving:       false,
    executing:       false,
    workspace:       false,
    activity:        false,
    metrics:         false,
    trends:          false,
  });

  readonly error = signal<string | null>(null);

  // ── Derived — Sprint 13A ──────────────────────────────────────────────────

  readonly proposedRecommendations = computed(() =>
    this.recommendations().filter(r => r.status === RecommendationStatus.Proposed),
  );

  readonly activeDrafts = computed(() =>
    this.drafts().filter(d => d.status === DraftStatus.Draft || d.status === DraftStatus.Reviewed),
  );

  readonly effectivePersona = computed(() =>
    this.personaOverride() ?? this.activePersona(),
  );

  // ── Derived — Sprint 13B ──────────────────────────────────────────────────

  readonly topRecommendations = computed(() =>
    this.proposedRecommendations()
      .sort((a, b) => a.priority - b.priority)
      .slice(0, 5),
  );

  readonly personaRecommendations = computed(() => {
    const persona = this.effectivePersona();
    return this.proposedRecommendations()
      .filter(r => r.persona === persona)
      .sort((a, b) => a.priority - b.priority)
      .slice(0, 5);
  });

  readonly urgentRecommendations = computed(() =>
    this.proposedRecommendations().filter(
      r => r.severity === RecommendationSeverity.Critical || r.severity === RecommendationSeverity.High,
    ).sort((a, b) =>
      (SEVERITY_ORDER[b.severity] ?? 0) - (SEVERITY_ORDER[a.severity] ?? 0)
      || a.priority - b.priority,
    ),
  );

  readonly recommendationCounts = computed(() => {
    const proposed = this.proposedRecommendations();
    return {
      total:    proposed.length,
      critical: proposed.filter(r => r.severity === RecommendationSeverity.Critical).length,
      high:     proposed.filter(r => r.severity === RecommendationSeverity.High).length,
      medium:   proposed.filter(r => r.severity === RecommendationSeverity.Medium).length,
      low:      proposed.filter(r => r.severity === RecommendationSeverity.Low).length,
    };
  });

  // ── Derived — Sprint 13C ──────────────────────────────────────────────────

  /** Counts of drafts by type. */
  readonly draftCounts = computed(() => {
    const active = this.activeDrafts();
    return {
      total:             active.length,
      capa:              active.filter(d => d.draftTypeEnum === DraftType.CapaDraft).length,
      executiveBriefing: active.filter(d => d.draftTypeEnum === DraftType.ExecutiveComplianceBriefing).length,
      readiness:         active.filter(d => d.draftTypeEnum === DraftType.ReadinessSummary).length,
      evidenceChecklist: active.filter(d => d.draftTypeEnum === DraftType.EvidenceChecklist).length,
    };
  });

  /** Latest version of each draft group (de-duplicates group versions). */
  readonly latestDraftsByGroup = computed(() => {
    const byGroup = new Map<string, AiDraft>();
    for (const draft of this.drafts()) {
      const existing = byGroup.get(draft.groupId);
      if (!existing || draft.version > existing.version) {
        byGroup.set(draft.groupId, draft);
      }
    }
    return Array.from(byGroup.values())
      .sort((a, b) => new Date(b.updatedUtc).getTime() - new Date(a.updatedUtc).getTime());
  });

  // ── Derived — Sprint 13D ──────────────────────────────────────────────────

  readonly pendingApprovals = computed(() =>
    this.proposals().filter(p => p.statusEnum === ProposalStatus.Proposed && !p.isExpired),
  );

  readonly approvedProposals = computed(() =>
    this.proposals().filter(p => p.statusEnum === ProposalStatus.Approved),
  );

  readonly rejectedProposals = computed(() =>
    this.proposals().filter(p => p.statusEnum === ProposalStatus.Rejected),
  );

  readonly executedProposals = computed(() =>
    this.proposals().filter(p => p.statusEnum === ProposalStatus.Executed),
  );

  readonly approvalCounts = computed(() => ({
    total:    this.proposals().length,
    pending:  this.pendingApprovals().length,
    approved: this.approvedProposals().length,
    rejected: this.rejectedProposals().length,
    executed: this.executedProposals().length,
    expired:  this.proposals().filter(p => p.statusEnum === ProposalStatus.Expired).length,
  }));

  readonly personaWorkspaceDrafts = computed(() =>
    this.displayWorkspace().highlightedDrafts ?? [],
  );

  readonly personaWorkspaceProposals = computed(() =>
    this.displayWorkspace().highlightedProposals ?? [],
  );

  readonly personaSuggestedQuestions = computed(() =>
    this.displayWorkspace().suggestedQuestions ?? [],
  );

  /** Workspace from API, or client-built fallback so the Overview always renders. */
  readonly displayWorkspace = computed((): PersonaWorkspace => {
    const persona = this.effectivePersona();
    const apiWs = this.personaWorkspace();
    const base = apiWs ?? this.buildFallbackWorkspace(persona);
    const personaRecs = this.personaRecommendations();
    const drafts = this.latestDraftsByGroup();

    return {
      ...base,
      prioritizedRecommendations:
        apiWs?.prioritizedRecommendations?.length
          ? apiWs.prioritizedRecommendations
          : personaRecs.slice(0, 5),
      highlightedDrafts:
        apiWs?.highlightedDrafts?.length
          ? apiWs.highlightedDrafts
          : drafts.slice(0, 5),
      landing: {
        ...base.landing,
        headline:
          apiWs?.landing.headline
          ?? personaRecs[0]?.title
          ?? base.landing.headline,
      },
    };
  });

  // ── Recommendation Actions ─────────────────────────────────────────────────

  loadRecommendations(): void {
    this.loading.update(s => ({ ...s, recommendations: true }));
    this.error.set(null);

    this.api.getRecommendations().pipe(
      catchError(err => {
        this.error.set(err?.message ?? 'Failed to load recommendations');
        this.loading.update(s => ({ ...s, recommendations: false }));
        return EMPTY;
      }),
    ).subscribe(items => {
      this.recommendations.set(items);
      this.loading.update(s => ({ ...s, recommendations: false }));
    });
  }

  loadPersonaRecommendations(persona?: string): void {
    const targetPersona = persona ?? this.effectivePersona();
    this.loading.update(s => ({ ...s, recommendations: true }));
    this.error.set(null);

    this.api.getRecommendationsByPersona(targetPersona).pipe(
      catchError(err => {
        this.error.set(err?.message ?? 'Failed to load persona recommendations');
        this.loading.update(s => ({ ...s, recommendations: false }));
        return EMPTY;
      }),
    ).subscribe(items => {
      const existing = this.recommendations().filter(r => r.persona !== targetPersona);
      this.recommendations.set([...existing, ...items]);
      this.loading.update(s => ({ ...s, recommendations: false }));
    });
  }

  refreshRecommendations(frameworkId?: string): void {
    this.loading.update(s => ({ ...s, refresh: true }));
    this.error.set(null);

    this.api.refreshRecommendations({ frameworkId, persona: this.effectivePersona() }).pipe(
      catchError(err => {
        this.error.set(err?.message ?? 'Failed to refresh recommendations');
        this.loading.update(s => ({ ...s, refresh: false }));
        return EMPTY;
      }),
    ).subscribe(items => {
      this.recommendations.set(items);
      this.loading.update(s => ({ ...s, refresh: false }));
    });
  }

  // ── Draft Actions — Sprint 13C ────────────────────────────────────────────

  loadDrafts(): void {
    this.loading.update(s => ({ ...s, drafts: true }));
    this.error.set(null);

    this.api.getDrafts().pipe(
      catchError(err => {
        this.error.set(err?.message ?? 'Failed to load drafts');
        this.loading.update(s => ({ ...s, drafts: false }));
        return EMPTY;
      }),
    ).subscribe(items => {
      this.drafts.set(items);
      this.loading.update(s => ({ ...s, drafts: false }));
    });
  }

  selectDraft(draft: AiDraft): void {
    this.activeDraft.set(draft);
  }

  clearActiveDraft(): void {
    this.activeDraft.set(null);
    this.draftVersions.set([]);
  }

  /** Generates a new draft and adds it to the drafts list. Sprint 13C. */
  generateDraft(request: GenerateDraftRequest): void {
    this.loading.update(s => ({ ...s, generating: true }));
    this.error.set(null);

    this.api.generateDraft(request).pipe(
      catchError(err => {
        this.error.set(err?.message ?? 'Failed to generate draft');
        this.loading.update(s => ({ ...s, generating: false }));
        return EMPTY;
      }),
    ).subscribe(draft => {
      this.drafts.update(existing => [draft, ...existing]);
      this.activeDraft.set(draft);
      this.loading.update(s => ({ ...s, generating: false }));
    });
  }

  /** Generates a draft directly from a recommendation. Sprint 13C. */
  generateDraftFromRecommendation(recommendation: CopilotRecommendation): void {
    const draftType = recommendationCategoryToDraftType(recommendation.category);
    this.generateDraft({
      draftType,
      sourceType:        DraftSourceType.Recommendation,
      persona:           recommendation.persona,
      recommendationId:  recommendation.id,
      sourceEntityId:    recommendation.sourceEntityId ?? undefined,
    });
  }

  /** Regenerates an existing draft (creates new version). Sprint 13C. */
  regenerateDraft(draftId: string): void {
    this.loading.update(s => ({ ...s, generating: true }));
    this.error.set(null);

    this.api.regenerateDraft(draftId).pipe(
      catchError(err => {
        this.error.set(err?.message ?? 'Failed to regenerate draft');
        this.loading.update(s => ({ ...s, generating: false }));
        return EMPTY;
      }),
    ).subscribe(newDraft => {
      this.drafts.update(existing => [newDraft, ...existing]);
      this.activeDraft.set(newDraft);
      this.loading.update(s => ({ ...s, generating: false }));
    });
  }

  /** Loads all versions of the active draft's group. Sprint 13C. */
  loadDraftVersions(draftId: string): void {
    this.loading.update(s => ({ ...s, versions: true }));

    this.api.getDraftVersions(draftId).pipe(
      catchError(() => {
        this.loading.update(s => ({ ...s, versions: false }));
        return EMPTY;
      }),
    ).subscribe(versions => {
      this.draftVersions.set(versions);
      this.loading.update(s => ({ ...s, versions: false }));
    });
  }

  // ── Persona Actions ───────────────────────────────────────────────────────

  loadPersonaCatalog(): void {
    this.loading.update(s => ({ ...s, catalog: true }));

    this.api.getPersonas().pipe(
      catchError(() => {
        this.loading.update(s => ({ ...s, catalog: false }));
        return EMPTY;
      }),
    ).subscribe(catalog => {
      this.personaCatalog.set(catalog);
      this.loading.update(s => ({ ...s, catalog: false }));
    });
  }

  resolvePersona(pageContext?: string, focusedEntityType?: string, focusedEntityId?: string): void {
    this.loading.update(s => ({ ...s, persona: true }));

    this.api.getActivePersona({
      pageContext,
      focusedEntityType,
      focusedEntityId,
      personaOverride: this.personaOverride() ?? undefined,
    }).pipe(
      catchError(() => {
        this.loading.update(s => ({ ...s, persona: false }));
        return EMPTY;
      }),
    ).subscribe(info => {
      const persona = info.id as CopilotPersona;
      if (!this.personaOverride()) {
        this.activePersona.set(persona);
      }
      this.loading.update(s => ({ ...s, persona: false }));
      this.loadPersonaWorkspace(this.effectivePersona());
    });
  }

  applySessionOverride(persona: CopilotPersona): void {
    this.personaOverride.set(persona);
    this.activePersona.set(persona);
  }

  overridePersona(persona: CopilotPersona): void {
    this.personaOverride.set(persona);
    this.activePersona.set(persona);
    try {
      sessionStorage.setItem(PERSONA_OVERRIDE_SESSION_KEY, persona);
    } catch { /* ignore */ }
    this.recordActivity(COPILOT_ACTIVITY.PersonaChanged, { persona });
    this.loadPersonaWorkspace(persona);
  }

  clearPersonaOverride(pageContext?: string, focusedEntityType?: string, focusedEntityId?: string): void {
    this.personaOverride.set(null);
    try {
      sessionStorage.removeItem(PERSONA_OVERRIDE_SESSION_KEY);
    } catch { /* ignore */ }
    this.resolvePersona(pageContext, focusedEntityType, focusedEntityId);
  }

  loadExperienceCatalog(): void {
    if (this.experienceCatalog().length > 0) return;
    this.api.getExperienceCatalog().pipe(catchError(() => EMPTY)).subscribe(catalog => {
      this.experienceCatalog.set(catalog);
    });
  }

  loadPersonaWorkspace(persona?: CopilotPersona): void {
    const target = persona ?? this.effectivePersona();
    this.loading.update(s => ({ ...s, workspace: true }));

    this.api.getPersonaWorkspace(target, this.complianceContext.getFrameworkId() ?? undefined).pipe(
      catchError(() => {
        this.personaWorkspace.set(null);
        this.loading.update(s => ({ ...s, workspace: false }));
        return EMPTY;
      }),
    ).subscribe(workspace => {
      this.personaWorkspace.set(workspace);
      this.loading.update(s => ({ ...s, workspace: false }));
    });
  }

  private buildFallbackWorkspace(persona: CopilotPersona): PersonaWorkspace {
    const definition =
      this.experienceCatalog().find(d => d.persona === persona)
      ?? this.experienceCatalog()[0]
      ?? CopilotStore.fallbackPersonaDefinition(persona);

    const topRec = this.personaRecommendations()[0];

    return {
      definition,
      landing: {
        persona,
        headline: topRec?.title ?? 'Review top compliance priorities for your organization.',
        subheadline: definition.description,
        readinessScore: null,
      },
      prioritizedRecommendations: [],
      highlightedDrafts: [],
      highlightedProposals: [],
      suggestedQuestions: definition.defaultQuestions.length > 0
        ? definition.defaultQuestions
        : CopilotStore.defaultSuggestedQuestions,
    };
  }

  private static readonly defaultSuggestedQuestions = [
    'What is blocking my accreditation?',
    'What should I prioritize today?',
    'Show critical deficiencies',
    'How do I improve readiness?',
  ];

  private static fallbackPersonaDefinition(persona: CopilotPersona): PersonaExperienceDefinition {
    const displayName = PERSONA_DISPLAY_NAMES[persona] ?? 'Compliance Copilot';
    return {
      persona,
      displayName,
      description: 'Persona-driven recommendations, drafts, and approvals for your compliance program.',
      icon: 'sparkles',
      theme: 'default',
      primaryObjectives: ['Readiness', 'Evidence coverage', 'Risk reduction'],
      recommendedActions: ['Generate Readiness Summary', 'Review critical deficiencies'],
      defaultQuestions: CopilotStore.defaultSuggestedQuestions,
      priorityCategories: [],
      supportedDraftTypes: [],
      supportedProposalTypes: [],
    };
  }

  loadActivityFeed(limit = 50): void {
    this.loading.update(s => ({ ...s, activity: true }));
    this.api.getActivityFeed(limit).pipe(
      catchError(() => {
        this.loading.update(s => ({ ...s, activity: false }));
        return EMPTY;
      }),
    ).subscribe(events => {
      this.activityFeed.set(events);
      this.loading.update(s => ({ ...s, activity: false }));
    });
  }

  loadUsageMetrics(): void {
    this.loading.update(s => ({ ...s, metrics: true }));
    this.api.getUsageMetrics().pipe(
      catchError(() => {
        this.loading.update(s => ({ ...s, metrics: false }));
        return EMPTY;
      }),
    ).subscribe(metrics => {
      this.usageMetrics.set(metrics);
      this.loading.update(s => ({ ...s, metrics: false }));
    });
  }

  loadAnalyticsTrends(days = 14): void {
    this.loading.update(s => ({ ...s, trends: true }));
    this.api.getAnalyticsTrends(days).pipe(
      catchError(() => {
        this.loading.update(s => ({ ...s, trends: false }));
        return EMPTY;
      }),
    ).subscribe(trends => {
      this.analyticsTrends.set(trends);
      this.loading.update(s => ({ ...s, trends: false }));
    });
  }

  recordActivity(
    eventType: string,
    opts?: { persona?: string; entityType?: string; entityId?: string; notes?: string },
  ): void {
    this.api.recordActivity({
      eventType,
      persona: opts?.persona ?? this.effectivePersona(),
      entityType: opts?.entityType,
      entityId: opts?.entityId,
      notes: opts?.notes,
    }).pipe(catchError(() => EMPTY)).subscribe();
  }

  setWorkspaceTab(tab: 'overview' | 'recommendations' | 'drafts' | 'approvals' | 'history' | 'analytics'): void {
    this.activeWorkspaceTab.set(tab);
  }

  setCopilotContext(context: CopilotContext): void {
    this.copilotContext.set(context);
  }

  // ── Proposal Actions — Sprint 13D ─────────────────────────────────────────

  loadProposals(): void {
    this.loading.update(s => ({ ...s, proposals: true }));
    this.error.set(null);

    this.api.getProposals().pipe(
      catchError(err => {
        this.error.set(err?.message ?? 'Failed to load proposals');
        this.loading.update(s => ({ ...s, proposals: false }));
        return EMPTY;
      }),
    ).subscribe(items => {
      this.proposals.set(items);
      this.loading.update(s => ({ ...s, proposals: false }));
    });
  }

  loadPendingProposals(): void {
    this.loading.update(s => ({ ...s, proposals: true }));
    this.error.set(null);

    this.api.getPendingProposals().pipe(
      catchError(err => {
        this.error.set(err?.message ?? 'Failed to load pending proposals');
        this.loading.update(s => ({ ...s, proposals: false }));
        return EMPTY;
      }),
    ).subscribe(items => {
      this.proposals.set(items);
      this.loading.update(s => ({ ...s, proposals: false }));
    });
  }

  selectProposal(proposal: CopilotActionProposal): void {
    this.activeProposal.set(proposal);
  }

  clearActiveProposal(): void {
    this.activeProposal.set(null);
  }

  /** Generates a proposal from a recommendation. */
  generateProposalFromRecommendation(
    recommendationId: string,
    request?: GenerateProposalFromRecommendationRequest,
  ): void {
    this.loading.update(s => ({ ...s, generating: true }));
    this.error.set(null);

    this.api.generateProposalFromRecommendation(recommendationId, request).pipe(
      catchError(err => {
        this.error.set(err?.message ?? 'Failed to generate proposal');
        this.loading.update(s => ({ ...s, generating: false }));
        return EMPTY;
      }),
    ).subscribe(proposal => {
      this.proposals.update(existing => [proposal, ...existing]);
      this.activeProposal.set(proposal);
      this.loading.update(s => ({ ...s, generating: false }));
    });
  }

  /** Generates a proposal from a draft. */
  generateProposalFromDraft(draftId: string): void {
    this.loading.update(s => ({ ...s, generating: true }));
    this.error.set(null);

    this.api.generateProposalFromDraft(draftId).pipe(
      catchError(err => {
        this.error.set(err?.message ?? 'Failed to generate proposal from draft');
        this.loading.update(s => ({ ...s, generating: false }));
        return EMPTY;
      }),
    ).subscribe(proposal => {
      this.proposals.update(existing => [proposal, ...existing]);
      this.activeProposal.set(proposal);
      this.loading.update(s => ({ ...s, generating: false }));
    });
  }

  /** Human approves an action proposal. */
  approveProposal(id: string, request?: ApproveProposalRequest): void {
    this.loading.update(s => ({ ...s, approving: true }));
    this.error.set(null);

    this.api.approveProposal(id, request).pipe(
      catchError(err => {
        this.error.set(err?.message ?? 'Failed to approve proposal');
        this.loading.update(s => ({ ...s, approving: false }));
        return EMPTY;
      }),
    ).subscribe(updated => {
      this.proposals.update(list => list.map(p => p.id === id ? updated : p));
      if (this.activeProposal()?.id === id) {
        this.activeProposal.set(updated);
      }
      this.loading.update(s => ({ ...s, approving: false }));
    });
  }

  /** Human rejects an action proposal. */
  rejectProposal(id: string, request: RejectProposalRequest): void {
    this.loading.update(s => ({ ...s, approving: true }));
    this.error.set(null);

    this.api.rejectProposal(id, request).pipe(
      catchError(err => {
        this.error.set(err?.message ?? 'Failed to reject proposal');
        this.loading.update(s => ({ ...s, approving: false }));
        return EMPTY;
      }),
    ).subscribe(updated => {
      this.proposals.update(list => list.map(p => p.id === id ? updated : p));
      if (this.activeProposal()?.id === id) {
        this.activeProposal.set(updated);
      }
      this.loading.update(s => ({ ...s, approving: false }));
    });
  }

  /** Executes an approved proposal — dispatches to existing workflow. */
  executeProposal(id: string): void {
    this.loading.update(s => ({ ...s, executing: true }));
    this.error.set(null);

    this.api.executeProposal(id).pipe(
      catchError(err => {
        this.error.set(err?.message ?? 'Failed to execute proposal');
        this.loading.update(s => ({ ...s, executing: false }));
        return EMPTY;
      }),
    ).subscribe(_result => {
      // Reload the proposal to get updated status
      this.api.getProposal(id).pipe(
        catchError(() => EMPTY),
      ).subscribe(updated => {
        this.proposals.update(list => list.map(p => p.id === id ? updated : p));
        if (this.activeProposal()?.id === id) {
          this.activeProposal.set(updated);
        }
        this.loading.update(s => ({ ...s, executing: false }));
      });
    });
  }

  loadProposalAuditTrail(proposalId: string): void {
    this.api.getProposalAuditTrail(proposalId).pipe(
      catchError(() => EMPTY),
    ).subscribe(events => {
      this.proposalAuditTrail.set(events);
    });
  }

  reset(): void {
    this.recommendations.set([]);
    this.drafts.set([]);
    this.activeDraft.set(null);
    this.draftVersions.set([]);
    this.proposals.set([]);
    this.activeProposal.set(null);
    this.proposalAuditTrail.set([]);
    this.personaOverride.set(null);
    this.activePersona.set(CopilotPersona.AccreditationCoach);
    this.personaWorkspace.set(null);
    this.activityFeed.set([]);
    this.usageMetrics.set(null);
    this.error.set(null);
  }
}
