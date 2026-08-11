import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { combineLatest, debounceTime, distinctUntilChanged, skip } from 'rxjs';
import {
  AiAnswerMode,
  AiAssistantApiService,
  AiResponseDto,
  AiSourceReference,
  CompliancePresentationDto,
} from '../../features/control-center/services/ai-assistant-api.service';
import { resolveDashboardCompletionRatePercent } from '../../features/dashboard/utils/dashboard-readiness.util';
import {
  DashboardSnapshot,
  SnapshotQueryParams,
} from '../../features/dashboard/models/dashboard-snapshot.model';
import { ProgramOverviewSnapshotService } from '../services/program-overview-snapshot.service';
import { ComplianceContextService } from '../services/compliance-context.service';
import { AiPageContextService } from '../services/ai-page-context.service';
import { environment } from '../../../environments/environment';

export type SidebarAiAssistantState = 'initial' | 'thinking' | 'chat' | 'error';

export interface SidebarAiSnapshotCard {
  id: 'open-findings' | 'issues-risks' | 'pending-tasks' | 'evidence-uploaded';
  title: string;
  value: string;
  tone: 'danger' | 'warning' | 'info' | 'success';
  prompt: string;
}

export interface SidebarAiSuggestion {
  id: string;
  text: string;
  tone: 'info' | 'warning' | 'success' | 'brand';
  prompt: string;
}

export interface SidebarAiCitation {
  label: string;
  excerpt?: string | null;
  sourceType?: string;
  relevanceScore?: number;
}

export interface SidebarAiTurn {
  id: string;
  prompt: string;
  createdAt: Date;
  contextLabel: string | null;
  status: 'thinking' | 'complete' | 'error';
  responseText?: string;
  reasoning?: string[];
  confidence?: number | null;
  sources?: string[];
  citations?: SidebarAiCitation[];
  followUps?: string[];
  errorMessage?: string;
  answerModeLabel?: string | null;
  /** Executive Compliance Advisor card model. When present it is the source of truth for rendering. */
  presentation?: CompliancePresentationDto | null;
}

@Injectable({ providedIn: 'root' })
export class SidebarAiAssistantStateService {
  private readonly aiApi = inject(AiAssistantApiService);
  private readonly programOverviewSnapshot = inject(ProgramOverviewSnapshotService);
  private readonly complianceContext = inject(ComplianceContextService);
  private readonly pageContext = inject(AiPageContextService);
  private readonly destroyRef = inject(DestroyRef);

  readonly uiState = signal<SidebarAiAssistantState>('initial');
  readonly turns = signal<SidebarAiTurn[]>([]);
  readonly activeInput = signal('');
  readonly loadingSnapshot = signal(true);
  readonly snapshot = signal<DashboardSnapshot | null>(null);
  readonly lastError = signal<string | null>(null);
  readonly pendingTurnId = signal<string | null>(null);
  readonly lastFailedTurnId = signal<string | null>(null);

  readonly welcomeChips = signal<SidebarAiSuggestion[]>([
    {
      id: 'chip-audit-readiness',
      text: 'Audit Readiness',
      tone: 'info',
      prompt: 'Summarize current audit readiness and top gaps.',
    },
    {
      id: 'chip-standards',
      text: 'Standards',
      tone: 'brand',
      prompt: 'Explain the key standards we are currently weakest on.',
    },
    {
      id: 'chip-risk-analysis',
      text: 'Risk Analysis',
      tone: 'warning',
      prompt: 'Analyze the top compliance risks and their likely impact.',
    },
    {
      id: 'chip-evidence',
      text: 'Evidence',
      tone: 'success',
      prompt: 'List evidence gaps that most affect compliance score.',
    },
  ]);

  /** Knowledge-search source chips for the references panel. */
  readonly knowledgeSourceChips = signal<SidebarAiSuggestion[]>([]);

  readonly suggestedActions = signal<SidebarAiSuggestion[]>([
    {
      id: 'suggest-upcoming-audit',
      text: 'Prepare for the upcoming NABH Surveillance Audit checklist.',
      tone: 'info',
      prompt: 'Prepare an action checklist for the upcoming NABH surveillance audit.',
    },
    {
      id: 'suggest-infection-risk',
      text: 'Analyze high-risk areas in the Infection Control department.',
      tone: 'warning',
      prompt: 'Analyze high-risk compliance areas in Infection Control.',
    },
    {
      id: 'suggest-training-evidence',
      text: 'Upload pending staff training records for Fire Safety.',
      tone: 'success',
      prompt: 'Which Fire Safety training records are still pending evidence upload?',
    },
    {
      id: 'suggest-jci-patient-safety',
      text: 'Interpret the new JCI Patient Safety Goal requirements.',
      tone: 'brand',
      prompt: 'Interpret the latest JCI Patient Safety Goal requirements for our context.',
    },
    {
      id: 'suggest-self-assessment',
      text: 'Complete the outstanding self-assessment for Medication Management.',
      tone: 'brand',
      prompt: 'Help me complete outstanding self-assessment for Medication Management.',
    },
  ]);

  readonly helperText = 'You can ask about audit readiness, risks, or compliance gaps.';

  readonly isBusy = computed(() => this.uiState() === 'thinking');
  readonly hasConversation = computed(() => this.turns().length > 0);
  readonly canSend = computed(() => this.activeInput().trim().length > 0 && !this.isBusy());

  readonly snapshotCards = computed<SidebarAiSnapshotCard[]>(() => {
    const snap = this.snapshot();
    const summary = snap?.summary;
    const tasks = snap?.tasksSummary;
    const alerts = snap?.alerts ?? [];
    const unackAlerts = alerts.filter((a) => !a.acknowledged);
    const highOrCriticalAlerts = unackAlerts.filter((a) => {
      const sev = a.severity?.toLowerCase();
      return sev === 'high' || sev === 'critical';
    });
    const openFindingsCount = tasks?.overdue ?? summary?.overdueTasks ?? 0;
    const issuesRisksCount =
      highOrCriticalAlerts.length > 0 ? highOrCriticalAlerts.length : unackAlerts.length;
    const pendingTaskCount = tasks?.open ?? tasks?.total ?? 0;
    const completionRatePct = resolveDashboardCompletionRatePercent(snap);

    return [
      {
        id: 'open-findings',
        title: 'Open Findings',
        value: String(openFindingsCount),
        tone: 'danger',
        prompt: 'Summarize current open findings',
      },
      {
        id: 'issues-risks',
        title: 'Issues & Risks',
        value: String(issuesRisksCount),
        tone: 'warning',
        prompt: 'Analyze top risks',
      },
      {
        id: 'pending-tasks',
        title: 'Pending Tasks',
        value: String(pendingTaskCount),
        tone: 'info',
        prompt: 'What tasks are pending and why?',
      },
      {
        id: 'evidence-uploaded',
        title: 'Evidence Uploaded',
        value: `${completionRatePct}%`,
        tone: 'success',
        prompt: 'What evidence is missing?',
      },
    ];
  });

  constructor() {
    this.refreshSnapshot();
    this.refreshKnowledgeChips('compliance evidence CAPA audit readiness');
    combineLatest([this.complianceContext.hospitalId$, this.complianceContext.frameworkId$])
      .pipe(
        skip(1),
        debounceTime(100),
        distinctUntilChanged((prev, curr) => prev[0] === curr[0] && prev[1] === curr[1]),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => {
        this.refreshSnapshot();
        this.refreshKnowledgeChips('compliance evidence CAPA audit readiness');
      });
  }

  setInput(value: string): void {
    this.activeInput.set(value);
  }

  sendTypedMessage(): void {
    const prompt = this.activeInput().trim();
    if (!prompt || this.isBusy()) {
      return;
    }
    this.activeInput.set('');
    this.sendPrompt(prompt);
  }

  sendPrompt(prompt: string): void {
    const cleanPrompt = prompt.trim();
    if (!cleanPrompt || this.isBusy()) {
      return;
    }

    const turnId = this.newId('turn');
    const turn: SidebarAiTurn = {
      id: turnId,
      prompt: cleanPrompt,
      createdAt: new Date(),
      contextLabel: this.currentContextLabel(),
      status: 'thinking',
    };
    this.turns.update((items) => [...items, turn]);
    this.pendingTurnId.set(turnId);
    this.lastFailedTurnId.set(null);
    this.lastError.set(null);
    this.uiState.set('thinking');

    this.aiApi
      .sendQuery(cleanPrompt, {
        conversationSummary: this.pageContext.buildConversationSummary(
          this.turns().filter((t) => t.status === 'complete'),
        ),
        recentEntityRefs: this.pageContext.getRecentEntityRefs(),
      })
      .subscribe({
        next: (response) => {
          this.applyResponse(turnId, response);
        },
        error: () => {
          this.applyError(turnId);
        },
      });
  }

  retryLastFailed(): void {
    if (this.isBusy()) {
      return;
    }
    const failedId = this.lastFailedTurnId();
    if (!failedId) {
      return;
    }
    const failedTurn = this.turns().find((t) => t.id === failedId);
    if (!failedTurn) {
      return;
    }
    this.turns.update((items) =>
      items.map((t) =>
        t.id === failedId
          ? {
              ...t,
              status: 'thinking',
              errorMessage: undefined,
            }
          : t,
      ),
    );
    this.pendingTurnId.set(failedId);
    this.lastError.set(null);
    this.uiState.set('thinking');
    this.aiApi
      .sendQuery(failedTurn.prompt, {
        conversationSummary: this.pageContext.buildConversationSummary(
          this.turns().filter((t) => t.status === 'complete'),
        ),
        recentEntityRefs: this.pageContext.getRecentEntityRefs(),
      })
      .subscribe({
        next: (response) => this.applyResponse(failedId, response),
        error: () => this.applyError(failedId),
      });
  }

  clearConversation(): void {
    this.turns.set([]);
    this.pendingTurnId.set(null);
    this.lastFailedTurnId.set(null);
    this.lastError.set(null);
    this.uiState.set('initial');
  }

  private applyResponse(turnId: string, response: AiResponseDto): void {
    const followUps = this.extractFollowUps(response);
    const sources = this.extractSources(response);
    const citations = this.extractCitations(response);
    const confidence = this.normalizeConfidence(
      response.grounded?.confidenceScore ??
        response.compositeConfidenceScore ??
        response.confidenceScore,
    );
    const responseText = this.sanitizeResponseText(
      response.grounded?.answer ?? response.complianceHint,
    );
    const answerModeLabel = this.answerModeLabel(
      response.answerMode ?? response.grounded?.answerMode,
    );
    const presentation = this.normalizePresentation(response.presentation);
    this.turns.update((items) =>
      items.map((turn) =>
        turn.id === turnId
          ? {
              ...turn,
              status: 'complete',
              responseText,
              confidence,
              reasoning: (response.grounded?.reasoning ?? response.reasoning ?? [])
                .map((r) => r.message)
                .filter((m) => !!m),
              sources,
              citations,
              followUps,
              answerModeLabel,
              presentation,
              errorMessage: undefined,
            }
          : turn,
      ),
    );
    this.pendingTurnId.set(null);
    this.lastFailedTurnId.set(null);
    this.lastError.set(null);
    this.uiState.set('chat');
    this.refreshKnowledgeChips(responseText.slice(0, 120) || 'compliance');
  }

  private applyError(turnId: string): void {
    const msg = "I couldn't process this. Please try again.";
    this.turns.update((items) =>
      items.map((turn) =>
        turn.id === turnId
          ? {
              ...turn,
              status: 'error',
              errorMessage: msg,
            }
          : turn,
      ),
    );
    this.pendingTurnId.set(null);
    this.lastFailedTurnId.set(turnId);
    this.lastError.set(msg);
    this.uiState.set('error');
  }

  private refreshSnapshot(): void {
    const params = this.getSnapshotParams();
    const cached = this.programOverviewSnapshot.getCached(params);
    if (cached) {
      this.snapshot.set(cached);
      this.loadingSnapshot.set(false);
      if (!this.programOverviewSnapshot.isFresh(params, environment.dashboardSnapshotCacheTtlMs)) {
        this.fetchSnapshot(params, true);
      }
      return;
    }

    this.loadingSnapshot.set(true);
    this.fetchSnapshot(params, false);
  }

  private fetchSnapshot(params: SnapshotQueryParams, background: boolean): void {
    this.programOverviewSnapshot.getProgramOverviewSnapshot(params, { background }).subscribe({
      next: (data) => {
        this.snapshot.set(data);
        this.loadingSnapshot.set(false);
      },
      error: () => {
        if (!background) {
          this.snapshot.set(null);
        }
        this.loadingSnapshot.set(false);
      },
    });
  }

  private getSnapshotParams(): SnapshotQueryParams {
    return {
      hospitalId: this.complianceContext.getHospitalId(),
      frameworkId: this.complianceContext.getFrameworkId(),
    };
  }

  private normalizePresentation(
    presentation: CompliancePresentationDto | null | undefined,
  ): CompliancePresentationDto | null {
    if (!presentation) {
      return null;
    }
    const hasContent =
      (presentation.executiveSummary ?? '').trim().length > 0 ||
      (presentation.findings?.length ?? 0) > 0 ||
      (presentation.risks?.length ?? 0) > 0 ||
      (presentation.recommendations?.length ?? 0) > 0;
    return hasContent ? presentation : null;
  }

  private normalizeConfidence(raw: number | undefined): number | null {
    if (typeof raw !== 'number' || Number.isNaN(raw)) {
      return null;
    }
    const asPercent = raw <= 1 ? raw * 100 : raw;
    return Math.max(0, Math.min(100, Math.round(asPercent)));
  }

  private extractFollowUps(response: AiResponseDto): string[] {
    const maybeSuggestions = (response as unknown as { followUpSuggestions?: unknown })
      .followUpSuggestions;
    if (!Array.isArray(maybeSuggestions)) {
      return [];
    }
    return maybeSuggestions
      .filter((entry): entry is string => typeof entry === 'string')
      .map((entry) => entry.trim())
      .filter((entry) => entry.length > 0)
      .slice(0, 4);
  }

  private extractSources(response: AiResponseDto): string[] {
    const refs = response.grounded?.citations?.length
      ? response.grounded.citations
      : (response.sourceReferences ?? []);
    return refs
      .map((source) => this.formatSource(source))
      .filter((entry) => entry.length > 0)
      .slice(0, 6);
  }

  private extractCitations(response: AiResponseDto): SidebarAiCitation[] {
    const refs = response.grounded?.citations?.length
      ? response.grounded.citations
      : (response.sourceReferences ?? []);
    const citations: SidebarAiCitation[] = [];
    for (const source of refs) {
      const label = this.formatSource(source);
      if (!label) {
        continue;
      }
      citations.push({
        label,
        excerpt: source.excerpt ?? source.summary ?? null,
        sourceType: source.sourceType,
        relevanceScore: source.relevanceScore,
      });
      if (citations.length >= 6) {
        break;
      }
    }
    return citations;
  }

  private refreshKnowledgeChips(query: string): void {
    try {
      this.aiApi.knowledgeSearch(query, { mode: 'hybrid', topK: 4 }).subscribe({
        next: (result) => {
          const chips: SidebarAiSuggestion[] = [];
          for (const [idx, c] of (result.citations ?? []).entries()) {
            const label = this.formatSource(c) || c.summary || `Source ${idx + 1}`;
            if (!label) {
              continue;
            }
            chips.push({
              id: `kb-${idx}-${c.sourceId || idx}`,
              text: label.length > 42 ? `${label.slice(0, 39)}…` : label,
              tone: 'info',
              prompt: `Explain this source and how it applies: ${label}`,
            });
            if (chips.length >= 4) {
              break;
            }
          }
          this.knowledgeSourceChips.set(chips);
        },
        error: () => {
          this.knowledgeSourceChips.set([]);
        },
      });
    } catch {
      this.knowledgeSourceChips.set([]);
    }
  }

  private formatSource(source: AiSourceReference): string {
    // Never surface internal "System / fallback" plumbing to the user.
    if (
      source.sourceType?.toLowerCase() === 'system' &&
      source.sourceId?.toLowerCase() === 'fallback'
    ) {
      return '';
    }
    if (source.sourceType?.toLowerCase() === 'model knowledge') {
      return 'General knowledge';
    }
    if (source.documentId) {
      const ver = source.version ? ` v${source.version}` : '';
      return `${source.sourceType || 'Document'} / ${source.documentId}${ver}`;
    }
    const parts = [source.sourceType, source.sourceId].filter((v) => !!v);
    return parts.join(' / ');
  }

  private answerModeLabel(mode: AiAnswerMode | undefined): string | null {
    switch (mode) {
      case 'GeneralKnowledge':
      case 'ComplianceKnowledge':
        return 'General knowledge';
      case 'OrganizationSpecific':
        return 'Workspace data';
      case 'Hybrid':
        return 'Workspace + general';
      default:
        return null;
    }
  }

  private sanitizeResponseText(hint: string | undefined): string {
    const text = (hint ?? '').trim();
    if (!text) {
      return 'No response text was provided.';
    }

    // Defensive: strip any internal RAG/retrieval failure language that slipped through.
    const forbidden = [
      /the context does not contain[^.?!]*[.?!]/gi,
      /context does not contain[^.?!]*[.?!]/gi,
      /facts object is empty[^.?!]*[.?!]?/gi,
      /no relevant facts[^.?!]*[.?!]?/gi,
      /retrieval returned[^.?!]*[.?!]?/gi,
      /vector search[^.?!]*[.?!]?/gi,
    ];
    let cleaned = text;
    for (const pattern of forbidden) {
      cleaned = cleaned.replace(pattern, '');
    }
    cleaned = cleaned.replace(/\s{2,}/g, ' ').trim();
    return cleaned.length >= 12
      ? cleaned
      : 'I can help with general healthcare compliance topics like NABH, JCI, ISO, HIPAA, CAPA, and FMS. Could you add a little more detail?';
  }

  private currentContextLabel(): string | null {
    const page = this.pageContext.snapshot();
    if (page.label) {
      return page.label;
    }
    if (page.pageContext !== 'General') {
      return `Based on ${page.pageContext}${page.entityType ? ` · ${page.entityType}` : ''}`;
    }
    const hospitalId = this.complianceContext.getHospitalId();
    const frameworkId = this.complianceContext.getFrameworkId();
    if (!hospitalId && !frameworkId) {
      return null;
    }
    if (hospitalId && frameworkId) {
      return `Hospital workspace · active framework`;
    }
    if (hospitalId) {
      return `Hospital workspace`;
    }
    return `Framework workspace`;
  }

  private newId(prefix: string): string {
    return `${prefix}-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
  }
}
