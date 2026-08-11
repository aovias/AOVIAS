import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Subject, forkJoin, EMPTY, TimeoutError, asyncScheduler, of, concat } from 'rxjs';
import { switchMap, catchError, map, finalize, throttleTime, tap } from 'rxjs/operators';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SessionService } from '../../../core/auth/session.service';
import { LoggerService } from '../../../core/services/logger.service';
import { NotificationService } from '../../../core/ui/services/notification.service';
import { environment } from '../../../../environments/environment';
import { NabhScore, OperationalMode } from '../models/nabh.enums';
import { JourneyChapter } from '../models/chapter.model';
import { JourneyWorkspaceSnapshot } from '../models/journey-tree.model';
import { ObjectiveElement, OeEvidenceItem, oeHasVerifiedEvidence } from '../models/objective-element.model';
import { ComplianceStandard } from '../models/standard.model';
import { JourneyMockWorkspaceService } from '../services/journey-mock-workspace.service';
import { JourneyDataService, JourneyModeQuery } from '../services/journey-data.service';
import { JourneyContextResolverService } from '../services/journey-context-resolver.service';
import {
  PatchObjectiveElementBody,
  PatchObjectiveElementResponseDto,
  ObjectiveJourneyStateDto,
  CompleteObjectiveJourneyRequest,
  isObjectiveJourneyStatusCompleted,
  objectiveJourneyWizardStatusToJourneyWorkflowStatus,
} from '../models/journey-api.model';
import {
  mergeJourneyWorkspaceFromSplit,
  mapObjectiveElementApiToDomain,
  mapOverviewMetricsApiToDomain,
  normOperationalMode,
} from '../mappers/journey-workspace.mapper';
import { filterOesByLifecycle, sortOesByCode } from '../utils/journey-filters';
import { JourneyDashboardContextVm } from '../models/journey-dashboard-vm.model';
import { JourneyContextPanelVm } from '../models/execution-screen-vm.model';
import {
  OeWizardPersistedState,
  OE_WIZARD_STEP_LABELS,
  splitAuditChecklistLines,
} from '../models/oe-execution-wizard.model';
import { oeFrequencyLabel, oeRowStatus } from '../utils/execution-oe-vm.util';
import { journeyWorkflowStatusLabel, oeJourneyStatus } from '../utils/journey-oe-status.util';
import { JourneyOperationalModePolicyContext } from '../models/journey-operational-mode-ui.model';
import {
  buildJourneyOperationalModeUiPolicy,
  operationalModeProgressionBlocked,
} from '../utils/journey-operational-mode.policy';
import { NodeDetailsDto } from '../models/knowledge-base-api.model';
import { KnowledgeBaseDataService } from '../services/knowledge-base-data.service';
import { JourneyCacheService } from '../services/journey-cache.service';
import { mapNodeDetailsToWizardVm } from '../utils/knowledge-base-oe-wizard.mapper';
import { formatEvidenceHttpError } from '../utils/evidence-http-error';
import { formatJourneyHttpError } from '../utils/journey-http-error';
import {
  CreateTaskDraftVm,
  createEmptyCreateTaskDraftVm,
  mergeTaskDraftFromSession,
} from '../models/create-task-draft-vm.model';
import { buildTaskDraftFromOeContext } from '../utils/build-task-draft-from-oe.util';
import { canonicalizeOeWizardPayload } from '../utils/oe-wizard-payload-canonicalize';

/** Quick filter for OE lists in execution workspace (composes with department filter). */
export type OeQuickFilter = 'all' | 'core_only' | 'non_compliant' | 'high_risk';

/**
 * Maturity stage filter — narrows the execution workspace to OEs of a specific
 * NABH classification band. Null means "all stages" (default full workspace view).
 */
export type MaturityStageFilter = 'core' | 'commitment' | 'achievement' | 'excellence' | null;

@Injectable()
export class JourneyWorkspaceStore {
  private readonly mock = inject(JourneyMockWorkspaceService);
  private readonly journeyData = inject(JourneyDataService);
  private readonly journeyCache = inject(JourneyCacheService);
  private readonly journeyContext = inject(JourneyContextResolverService);
  private readonly session = inject(SessionService);
  private readonly kb = inject(KnowledgeBaseDataService);
  private readonly notification = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly logger = inject(LoggerService);

  /** Serializes overlapping workspace forkJoin loads; newer journeyId supersedes (switchMap). */
  private readonly workspaceLoad$ = new Subject<{
    journeyId: string;
    modeQuery?: JourneyModeQuery;
    resetChapterStandards: boolean;
  }>();

  /** Throttled server draft sync (max ~1 PUT / 500ms while editing). */
  private readonly wizardPersistTrigger$ = new Subject<void>();

  /**
   * Latest chapter id wins: rapid selection switches this stream and aborts the prior GET (intentional).
   */
  private readonly chapterStandardsTrigger$ = new Subject<{
    chapterId: string;
    force?: boolean;
    reqId: number;
  }>();

  /** Per-chapter request id — stale HTTP responses are ignored after stage changes or forced reloads. */
  private readonly standardsRequestByChapter = new Map<string, number>();

  /** Cancels stale KB responses when OE changes or wizard resets. */
  private knowledgeLoadGeneration = 0;

  constructor() {
    this.wireWorkspaceLoad();
    this.wireChapterStandardsLoad();
    this.wireWizardAutosave();
  }

  private readonly _snapshot = signal<JourneyWorkspaceSnapshot | null>(null);
  readonly snapshot = this._snapshot.asReadonly();

  readonly loading = signal(false);
  readonly loadError = signal<string | null>(null);
  /** Chapter id while GET …/standards is in flight (execution panel). */
  readonly standardsLoadingChapterId = signal<string | null>(null);

  readonly selectedChapterId = signal<string | null>(null);
  readonly selectedStandardId = signal<string | null>(null);
  readonly selectedOeId = signal<string | null>(null);

  readonly lifecycleMode = signal<OperationalMode>(OperationalMode.SelfAssessment);
  readonly chapterSearch = signal('');
  /** When true, chapters sorted by risk (high first); else by code */
  readonly sortChaptersByRiskDesc = signal(false);

  /** Execution workspace: filter OE rows by owning department (`all` shows every OE). */
  readonly departmentFilter = signal<string>('all');

  /** Execution workspace: filter OEs by compliance / classification (composes with department filter). */
  readonly oeQuickFilter = signal<OeQuickFilter>('all');

  /**
   * Compliance Maturity stage filter — when set, the execution workspace shows only
   * OEs belonging to that NABH classification band (Core / Commitment / Achievement / Excellence).
   * Populated from `?stage=` query param or navigation from Compliance Maturity Dashboard.
   */
  readonly maturityStageFilter = signal<MaturityStageFilter>(null);

  readonly drawerOpen = signal(false);

  /** Create & Assign Task drawer (OE wizard quick action — Phase 59). */
  readonly taskAssignmentDrawerOpen = signal(false);
  private readonly _taskAssignmentDraft = signal<CreateTaskDraftVm>(createEmptyCreateTaskDraftVm());
  readonly taskAssignmentDraft = this._taskAssignmentDraft.asReadonly();

  /** OE ids with in-flight local mutations */
  readonly oeMutationInFlight = signal<ReadonlySet<string>>(new Set());

  readonly oeWizardOpen = signal(false);
  readonly oeWizardStepIndex = signal(0);
  readonly oeWizardDirty = signal(false);
  readonly wizardUnderstandAck = signal(false);
  readonly wizardExecuteReady = signal(false);
  /** Local gate before Audit Checklist (Phase 50). */
  readonly wizardEvidenceAck = signal(false);
  readonly wizardEvalSubmitted = signal(false);
  readonly wizardEvalScore = signal(3);
  readonly wizardEvalTone = signal<'compliant' | 'partial' | 'non'>('compliant');
  /** Required when eval tone is partial or non-compliant */
  readonly wizardEvalJustification = signal('');
  readonly wizardEvalJustificationError = signal(false);
  readonly wizardNeedsImprove = signal(false);
  readonly wizardImproveReady = signal(false);
  readonly wizardCompleteAck = signal(false);

  /** KB audit checklist line checks; reconciled when `auditChecklist` text changes (Phase 52). */
  readonly wizardAuditChecklistChecked = signal<boolean[]>([]);
  /** Trimmed KB `auditChecklist` text — detects edits for reconcile. */
  private wizardAuditChecklistContentKey = '';

  /** Server optimistic concurrency token (base64 rowversion). */
  readonly wizardRowVersion = signal<string | null>(null);

  /** When true, wizard is completed on server — no autosave, inputs locked. */
  readonly oeWizardRemoteCompleted = signal(false);

  /** Last successfully persisted canonical JSON (string compare only). */
  private readonly lastWizardCanonSent = signal<string>('');

  /** Knowledge Base `GET …/nodes/{id}/details` (and code fallback) for the selected OE wizard. */
  readonly knowledgeBaseLoading = signal(false);
  readonly knowledgeBaseError = signal<string | null>(null);
  readonly knowledgeBaseNodeDetails = signal<NodeDetailsDto | null>(null);

  readonly readOnly = computed(() => {
    const snap = this._snapshot();
    if (snap?.readOnly === true || snap?.snapshotReadOnly === true) return true;
    const m = this.lifecycleMode();
    return (
      m === OperationalMode.FinalAudit ||
      m === OperationalMode.Surveillance ||
      m === OperationalMode.ReAccreditation
    );
  });

  /** Server-driven accreditation snapshot from dashboard API (no client-side NABH evaluation). */
  readonly accreditationSnapshot = computed(() => this._snapshot()?.accreditation ?? null);

  readonly overview = computed(() => this._snapshot()?.overview ?? null);

  /** Per-stage compliance scores derived from the snapshot overview StageBreakdown. */
  readonly stages = computed(() => this._snapshot()?.overview?.stageBreakdown ?? []);

  /** Authoritative unlock state from StageUnlockingService (Sprint 1 — H2). */
  readonly maturityStages = computed(() => this._snapshot()?.maturityStages ?? []);

  readonly currentMaturityStage = computed(() => this._snapshot()?.currentMaturityStage ?? null);

  // Obligation-aware computed signals (Phase 4)

  /** Obligation maturity breakdown from overview - null when obligations not yet synced. */
  readonly obligationBreakdown = computed(
    () => this.overview()?.obligationBreakdown ?? null,
  );

  /**
   * Per-OE obligation state lookup - computed from the full workspace tree.
   * Returns a Map<oeId, ObligationStateApiDto> for O(1) access in OE row components.
   */
  readonly obligationStateByOeId = computed((): Map<string, import('../models/journey-api.model').ObligationStateApiDto> => {
    const map = new Map<string, import('../models/journey-api.model').ObligationStateApiDto>();
    const snap = this._snapshot();
    snap?.chapters?.forEach((ch) =>
      ch.standards?.forEach((s) =>
        s.objectiveElements?.forEach((oe) => {
          if (oe.obligationState) map.set(oe.id!, oe.obligationState);
        }),
      ),
    );
    return map;
  });

  /** Stage obligation summaries derived from enriched stage breakdown. */
  readonly stageObligationSummaries = computed(
    (): import('../models/journey-tree.model').StageObligationSummary[] =>
      this.stages().map((s) => ({
        stage: s.stage,
        total: s.totalObligations ?? 0,
        verified: s.verifiedObligations ?? 0,
        implemented: s.implementedObligations ?? 0,
        partial: s.partialObligations ?? 0,
        deficient: s.deficientObligations ?? 0,
        readinessScore: s.obligationReadinessScore ?? 0,
      })),
  );


  /** Surveillance “changed OE” for wizard Audit Checklist gating — from OE API when present. */
  readonly selectedOeChangedSinceLastAudit = computed((): boolean => {
    const id = this.selectedOeId();
    if (!id) return true;
    const found = this.findOe(id);
    if (found?.oe.changedSinceLastAudit != null) {
      return !!found.oe.changedSinceLastAudit;
    }
    let h = 0;
    for (const c of id) h = (h + c.charCodeAt(0)) % 7;
    return h !== 0;
  });

  readonly operationalModePolicyContext = computed((): JourneyOperationalModePolicyContext => {
    const snap = this._snapshot();
    const ov = this.overview();
    return {
      snapshotReadOnly: snap?.readOnly === true || snap?.snapshotReadOnly === true,
      workspaceReadOnly: this.readOnly(),
      coreCompliancePercent: ov?.coreCompliancePercent ?? 0,
      failedCoreCount: ov?.failedCoreCount ?? 0,
      selectedOeChangedSinceLastAudit: this.selectedOeChangedSinceLastAudit(),
    };
  });

  /** Centralized OperationalMode → UI visibility and interaction (Phase 48). */
  readonly operationalModeUi = computed(() =>
    buildJourneyOperationalModeUiPolicy(this.lifecycleMode(), this.operationalModePolicyContext()),
  );

  readonly wizardInputsLocked = computed(
    () => this.operationalModeUi().wizard.allStepsReadOnly || this.oeWizardRemoteCompleted(),
  );

  /**
   * Complete CTA: do not tie to `allStepsReadOnly` (e.g. Final Audit locks step edits but may still complete).
   * Still respect server-completed and score‑5 / verified-evidence rules.
   */
  readonly oeWizardMarkCompleteDisabled = computed(
    () =>
      this.oeWizardRemoteCompleted() || this.oeWizardCompleteBlockedNoVerifiedEvidence(),
  );

  /**
   * Full score (5) cannot be saved on complete without ≥1 verified evidence
   * (`JourneyWorkspaceAppService` / `PatchObjectiveElementAsync`).
   */
  readonly oeWizardCompleteBlockedNoVerifiedEvidence = computed(() => {
    if (this.oeWizardRemoteCompleted()) return false;
    if (this.wizardEvalScore() !== 5) return false;
    const id = this.selectedOeId();
    if (!id) return false;
    const found = this.findOe(id);
    if (!found) return false;
    return !oeHasVerifiedEvidence(found.oe);
  });

  readonly filteredChapters = computed((): JourneyChapter[] => {
    const snap = this._snapshot();
    if (!snap) return [];
    let list = [...snap.chapters];
    const q = this.chapterSearch().trim().toLowerCase();
    if (q) {
      list = list.filter(
        (c) => c.name.toLowerCase().includes(q) || c.code.toLowerCase().includes(q),
      );
    }
    // Maturity stage filtering applies to lazy-loaded standards/OEs (classificationFilter on
    // GET …/standards). The chapter index is always the full list — do not inspect empty
    // standards[] here or every chapter would disappear before standards load.
    const stageFilter = this.maturityStageFilter();
    if (stageFilter && environment.useJourneyMockWorkspace) {
      list = list.filter((c) =>
        c.standards.some((s) =>
          s.objectiveElements.some(
            (oe) => (oe.classification ?? '').toLowerCase() === stageFilter,
          ),
        ),
      );
    }
    if (this.sortChaptersByRiskDesc()) {
      const order: Record<string, number> = { high: 0, medium: 1, low: 2 };
      list.sort((a, b) => order[a.risk] - order[b.risk] || a.code.localeCompare(b.code));
    } else {
      list.sort((a, b) => a.code.localeCompare(b.code));
    }
    return list;
  });

  readonly selectedChapter = computed((): JourneyChapter | null => {
    const id = this.selectedChapterId();
    const snap = this._snapshot();
    if (!id || !snap) return null;
    return snap.chapters.find((c) => c.id === id) ?? null;
  });

  readonly standardsForSelectedChapter = computed((): ComplianceStandard[] => {
    return this.selectedChapter()?.standards ?? [];
  });

  readonly selectedStandard = computed((): ComplianceStandard | null => {
    const id = this.selectedStandardId();
    const snap = this._snapshot();
    if (!id || !snap) return null;
    for (const ch of snap.chapters) {
      const st = ch.standards.find((s) => s.id === id);
      if (st) return st;
    }
    return null;
  });

  readonly objectiveElementsForSelectedStandard = computed((): ObjectiveElement[] => {
    const st = this.selectedStandard();
    if (!st) return [];
    return this.filteredOesForStandard(st);
  });

  readonly selectedContext = computed<JourneyDashboardContextVm>(() => {
    const snap = this._snapshot();
    const selectedChapter = this.selectedChapter();
    const selectedStandard = this.selectedStandard();
    const selectedOeId = this.selectedOeId();
    const selectedOe = selectedStandard?.objectiveElements.find((oe) => oe.id === selectedOeId) ?? null;

    return {
      frameworkCode: snap?.frameworkCode ?? '',
      frameworkName: snap?.frameworkName ?? '',
      selectedChapterCode: selectedChapter?.code ?? null,
      selectedStandardCode: selectedStandard?.code ?? null,
      selectedOeCode: selectedOe?.code ?? null,
      chapterRisk: selectedChapter?.risk ?? null,
    };
  });

  /** Right-hand Journey Context panel (execution screen). */
  readonly journeyContextPanelVm = computed((): JourneyContextPanelVm => {
    const loading = this.loading();
    const readOnly = this.readOnly();
    const id = this.selectedOeId();
    if (!id) {
      return { empty: true, contextLoading: loading, readOnly };
    }
    const found = this.findOe(id);
    if (!found) {
      return { empty: true, contextLoading: loading, readOnly };
    }
    const oe = found.oe;
    const row = oeRowStatus(oe);
    const j = oeJourneyStatus(oe);
    const ev = oe.evidence ?? [];
    const lastEv = ev.length
      ? [...ev].sort((a, b) => b.uploadedAt.localeCompare(a.uploadedAt))[0]
      : null;
    const progressPercent = Math.round((oe.score / 5) * 100);
    const mode = this.lifecycleMode();
    const kpi =
      mode === OperationalMode.ReAccreditation
        ? 'Excellence band focus — review KPIs before sign-off.'
        : null;
    return {
      empty: false,
      contextLoading: loading,
      readOnly,
      kpiInsightLabel: kpi,
      progressPercent,
      code: oe.code,
      title: oe.title ?? oe.description,
      departmentLabel: oe.departmentLabel ?? null,
      assigneeDisplayName: oe.assigneeDisplayName ?? null,
      frequencyLabel: oeFrequencyLabel(oe),
      scoreLabel: `${oe.score}/5`,
      statusLabel: row.statusLabel,
      statusTone: row.statusTone,
      evidenceCountLabel: ev.length === 1 ? '1 file' : `${ev.length} files`,
      lastEvidenceUpdated: lastEv?.uploadedAt ?? null,
      journeyStatus: j,
      journeyStatusLabel: journeyWorkflowStatusLabel(j),
      obligationState: this.obligationStateByOeId().get(id) ?? null,
    };
  });

  /** OE compliance wizard (Phase 44 shell, Phase 50 inline UX). */
  readonly oeWizardProgress = computed(() => {
    const i = this.oeWizardStepIndex();
    return Math.round(((i + 1) / OE_WIZARD_STEP_LABELS.length) * 100);
  });

  /** UI-only heuristic (minutes). */
  readonly wizardEstimatedMinutesRemaining = computed(() => {
    if (!this.oeWizardOpen()) return 0;
    const i = this.oeWizardStepIndex();
    return Math.max(0, (OE_WIZARD_STEP_LABELS.length - i) * 3);
  });

  readonly oeWizardNextAction = computed((): string => {
    if (!this.oeWizardOpen()) return '';
    if (!this.wizardUnderstandAck()) {
      return 'Review explanation, then continue to compliance guidance';
    }
    if (!this.wizardExecuteReady()) {
      return 'Review how to comply and mark guidance complete';
    }
    if (!this.wizardEvidenceAck()) {
      return 'Attach or confirm evidence required for this OE';
    }
    if (!this.wizardEvalSubmitted()) {
      return 'Complete the audit checklist and submit evaluation';
    }
    if (this.wizardNeedsImprove() && !this.wizardImproveReady()) {
      return 'Resolve checklist items or mark ready for review';
    }
    if (!this.wizardCompleteAck()) {
      return 'Review summary and submit';
    }
    return 'Journey execution complete';
  });

  readonly oeWizardCanAccessStep = computed(() => {
    const policy = this.operationalModeUi().wizard;
    return (step: number): boolean => {
      if (!this.oeWizardOpen()) return false;
      if (this.oeWizardRemoteCompleted()) {
        return step >= 0 && step < OE_WIZARD_STEP_LABELS.length;
      }
      const interaction = policy.stepInteractions[step] ?? 'active';
      if (interaction === 'locked') return false;
      if (policy.blockStepNavigation) {
        return step === 0;
      }
      if (step === 0) return true;
      if (step === 1) return this.wizardUnderstandAck();
      if (step === 2) return this.wizardExecuteReady();
      if (step === 3) return this.wizardEvidenceAck();
      if (step === 4) {
        return (
          this.wizardEvalSubmitted() &&
          (!this.wizardNeedsImprove() || this.wizardImproveReady())
        );
      }
      return false;
    };
  });

  readonly oeWizardStepCompleted = computed(() => {
    return (step: number): boolean => {
      if (step === 0) return this.wizardUnderstandAck();
      if (step === 1) return this.wizardExecuteReady();
      if (step === 2) return this.wizardEvidenceAck();
      if (step === 3) {
        return (
          this.wizardEvalSubmitted() &&
          (!this.wizardNeedsImprove() || this.wizardImproveReady())
        );
      }
      if (step === 4) return this.wizardCompleteAck();
      return false;
    };
  });

  /** KB-derived copy for wizard steps (null when unavailable or mock workspace). */
  readonly oeWizardKnowledgeVm = computed(() => {
    const oid = this.selectedOeId();
    if (!oid) return null;
    const found = this.findOe(oid);
    if (!found) return null;
    return mapNodeDetailsToWizardVm(this.knowledgeBaseNodeDetails(), found.oe);
  });

  /** Breadcrumb + OE identity for wizard chrome. */
  readonly oeWizardHeaderVm = computed(() => {
    const oid = this.selectedOeId();
    if (!oid) return null;
    const found = this.findOe(oid);
    const snap = this._snapshot();
    if (!found || !snap) return null;
    const row = oeRowStatus(found.oe);
    const j = oeJourneyStatus(found.oe);
    return {
      frameworkCode: snap.frameworkCode ?? '',
      frameworkName: snap.frameworkName ?? '',
      chapterId: found.chapter.id,
      chapterCode: found.chapter.code,
      chapterTitle: found.chapter.name,
      standardId: found.standard.id,
      standardCode: found.standard.code,
      standardTitle: found.standard.title,
      oeCode: found.oe.code,
      oeTitle: found.oe.title ?? found.oe.description,
      statusLabel: row.statusLabel,
      statusTone: row.statusTone,
      journeyStatusLabel: journeyWorkflowStatusLabel(j),
      obligationState: this.obligationStateByOeId().get(oid) ?? null,
    };
  });

  initFromRoute(): void {
    const jid = this.journeyContext.resolveJourneyId();
    if (environment.useJourneyMockWorkspace) {
      const id = jid ?? 'mock-nabh-hospital-6';
      this.loadMockData(id);
      return;
    }
    if (!jid) {
      this.loadError.set(
        'No active journey could be resolved. Select a hospital and framework in the sidebar, then open Journey again.',
      );
      this.loading.set(false);
      this._snapshot.set(null);
      return;
    }
    this.loadWorkspaceFromApi(jid);
  }

  /** Reload session + dashboard + chapters (e.g. after hospital switch or refresh control). */
  refreshWorkspace(): void {
    const jid = this._snapshot()?.journeyId ?? this.journeyContext.resolveJourneyId();
    if (!jid) {
      this.initFromRoute();
      return;
    }
    if (environment.useJourneyMockWorkspace) {
      this.loadMockData(jid);
      return;
    }
    this.loadWorkspaceFromApi(jid);
  }

  retryLoad(): void {
    this.mock.clearCache();
    this.journeyData.clearWorkspaceCache();
    this.initFromRoute();
  }

  loadMockData(journeyId: string): void {
    this.loading.set(true);
    this.loadError.set(null);
    try {
      const snap = this.mock.getWorkspace(journeyId, this.lifecycleMode());
      this._snapshot.set(snap);
      const first = snap.chapters[0];
      this.selectedChapterId.set(first?.id ?? null);
      this.selectedStandardId.set(first?.standards[0]?.id ?? null);
      this.selectedOeId.set(null);
      this.loading.set(false);
    } catch {
      this.loadError.set('Could not load journey workspace.');
      this.loading.set(false);
      this._snapshot.set(null);
    }
  }

  private wireWorkspaceLoad(): void {
    this.workspaceLoad$
      .pipe(
        switchMap(({ journeyId, modeQuery, resetChapterStandards }) => {
          const requestedMode = modeQuery?.operationalMode ?? this.lifecycleMode();
          const stageKey = modeQuery?.classificationFilter ?? null;
          const cached =
            resetChapterStandards
              ? null
              : this.journeyCache.getWorkspace(journeyId, requestedMode, stageKey);
          // Never pass classificationFilter to the chapter index — standards load lazily per chapter.
          const workspaceModeQuery: JourneyModeQuery | undefined = modeQuery
            ? {
                operationalMode: modeQuery.operationalMode,
                auditLifecycleMode: modeQuery.auditLifecycleMode,
              }
            : undefined;

          const network$ = forkJoin({
            session: this.journeyData.getSession(journeyId, workspaceModeQuery),
            dashboard: this.journeyData.getDashboard(journeyId, workspaceModeQuery),
            chapters: this.journeyData.getChaptersIndex(journeyId, workspaceModeQuery),
          }).pipe(
            tap(({ session, dashboard, chapters }) => {
              const mode = normOperationalMode(session.operationalMode) ?? requestedMode;
              this.journeyCache.setWorkspace(journeyId, mode, session, dashboard, chapters, stageKey);
            }),
            map((result) => ({ ...result, journeyId, resetChapterStandards, cacheHit: false })),
            catchError((err: unknown) => {
              if (cached) {
                this.logJourneyHttpDev('workspace-refresh', err);
                return EMPTY;
              }
              this.loadError.set(formatJourneyHttpError(err));
              this.loading.set(false);
              this._snapshot.set(null);
              this.logJourneyHttpDev('workspace', err);
              return EMPTY;
            }),
          );

          if (!cached) {
            return network$;
          }

          const cached$ = of({
            session: cached.session,
            dashboard: cached.dashboard,
            chapters: cached.chapters,
            journeyId,
            resetChapterStandards,
            cacheHit: true,
          });
          return concat(cached$, network$);
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(({ session, dashboard, chapters, journeyId, resetChapterStandards }) => {
        let merged = mergeJourneyWorkspaceFromSplit(journeyId, session, dashboard, chapters);
        if (resetChapterStandards) {
          merged = {
            ...merged,
            chapters: merged.chapters.map((c) => ({ ...c, standards: [] })),
          };
          this._snapshot.set(merged);
          const lm = normOperationalMode(session.operationalMode);
          if (lm != null) {
            this.lifecycleMode.set(lm);
          }
          const first = merged.chapters[0];
          this.selectedChapterId.set(first?.id ?? null);
          this.selectedStandardId.set(null);
          this.selectedOeId.set(null);
          if (first?.id) {
            this.ensureChapterStandardsLoaded(first.id);
          }
          this.loading.set(false);
          return;
        }

        this._snapshot.set(merged);
        const lm = normOperationalMode(session.operationalMode);
        if (lm != null) {
          this.lifecycleMode.set(lm);
        }
        const first = merged.chapters[0];
        this.selectedChapterId.set(first?.id ?? null);
        this.selectedStandardId.set(null);
        this.selectedOeId.set(null);
        if (first?.id) {
          this.ensureChapterStandardsLoaded(first.id);
        }
        this.loading.set(false);
      });
  }

  private wireChapterStandardsLoad(): void {
    this.chapterStandardsTrigger$
      .pipe(
        switchMap(({ chapterId, force, reqId }) => {
          const snap = this._snapshot();
          if (!snap) return EMPTY;
          const ch = snap.chapters.find((c) => c.id === chapterId);
          if (!ch) return EMPTY;
          if (!force && ch.standards.length > 0) return EMPTY;
          const jid = snap.journeyId;
          const mode = this.lifecycleMode();
          const stageKey = this.maturityStageFilter();
          const cached = force
            ? null
            : this.journeyCache.getChapterStandards(jid, chapterId, mode, stageKey);
          if (cached) {
            return of({ chapterId, standards: cached, reqId });
          }
          const stageFilter = stageKey;
          const modeQ = {
            operationalMode: mode,
            ...(stageFilter ? { classificationFilter: stageFilter } : {}),
          };
          this.standardsLoadingChapterId.set(chapterId);
          return this.journeyData
            .getChapterStandards(jid, chapterId, { ...modeQ, page: 1, pageSize: 200 })
            .pipe(
              tap((standards) => {
                this.journeyCache.setChapterStandards(jid, chapterId, mode, standards, stageKey);
              }),
              map((standards) => ({ chapterId, standards, reqId })),
              catchError((err: unknown) => {
                this.logJourneyHttpDev('chapter-standards', err);
                this.notification.showError(formatJourneyHttpError(err));
                return EMPTY;
              }),
              finalize(() => {
                if (this.standardsLoadingChapterId() === chapterId) {
                  this.standardsLoadingChapterId.set(null);
                }
              }),
            );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(({ chapterId, standards, reqId }) => {
        if (this.standardsRequestByChapter.get(chapterId) !== reqId) {
          return;
        }
        if (this.selectedChapterId() !== chapterId) {
          return;
        }
        this.mergeStandardsIntoChapter(chapterId, standards);
        const sel = this.selectedStandardId();
        if (!sel) {
          const firstWithOes = standards.find((s) => s.objectiveElements.length > 0);
          const pick = firstWithOes ?? standards[0];
          if (pick) {
            this.selectedStandardId.set(pick.id);
          }
        }
      });
  }

  private wireWizardAutosave(): void {
    this.wizardPersistTrigger$
      .pipe(
        throttleTime(500, asyncScheduler, { leading: true, trailing: true }),
        switchMap(() => {
          if (this.oeWizardRemoteCompleted()) return EMPTY;
          const jid = this._snapshot()?.journeyId;
          const oid = this.selectedOeId();
          if (!jid || !oid || environment.useJourneyMockWorkspace || !this.oeWizardOpen()) {
            return EMPTY;
          }
          const payload = this.buildWizardPayloadObject();
          const canon = canonicalizeOeWizardPayload(payload);
          if (canon === this.lastWizardCanonSent()) return EMPTY;
          return this.journeyData
            .putOeExecutionWizardDraft(jid, oid, {
              payload,
              rowVersion: this.wizardRowVersion() ?? undefined,
            })
            .pipe(
              tap((dto) => {
                this.wizardRowVersion.set(dto.rowVersion);
                this.oeWizardRemoteCompleted.set(isObjectiveJourneyStatusCompleted(dto.status));
                this.lastWizardCanonSent.set(canonicalizeOeWizardPayload(dto.payload));
                const wid = this.selectedOeId();
                if (wid) {
                  this.patchObjectiveElementJourneyWorkflow(
                    wid,
                    objectiveJourneyWizardStatusToJourneyWorkflowStatus(dto.status),
                  );
                }
              }),
              catchError((err: unknown) => {
                const http = err as HttpErrorResponse;
                if (http.status === 409) {
                  this.notification.showError('Wizard was updated in another session. Reloading saved state.');
                  this.reloadWizardFromServer();
                } else {
                  this.logJourneyHttpDev('oe-wizard-draft', err);
                  this.notification.showError(formatJourneyHttpError(err));
                }
                return EMPTY;
              }),
            );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe();
  }

  private loadWorkspaceFromApi(journeyId: string): void {
    this.loading.set(true);
    this.loadError.set(null);
    // Chapter index is always unfiltered; maturity stage narrowing applies to lazy standards/OE GETs.
    this.workspaceLoad$.next({
      journeyId,
      modeQuery: undefined,
      resetChapterStandards: false,
    });
  }

  private ensureChapterStandardsLoaded(chapterId: string, force = false): void {
    const snap = this._snapshot();
    if (!snap) return;
    const ch = snap.chapters.find((c) => c.id === chapterId);
    if (!ch) return;
    if (!force && ch.standards.length > 0) return;
    if (force && ch.standards.length > 0) {
      this.mergeStandardsIntoChapter(chapterId, []);
    }
    const reqId = (this.standardsRequestByChapter.get(chapterId) ?? 0) + 1;
    this.standardsRequestByChapter.set(chapterId, reqId);
    this.chapterStandardsTrigger$.next({ chapterId, force, reqId });
  }

  private mergeStandardsIntoChapter(chapterId: string, standards: ComplianceStandard[]): void {
    this._snapshot.update((s) => {
      if (!s) return s;
      return {
        ...s,
        chapters: s.chapters.map((c) => (c.id === chapterId ? { ...c, standards } : c)),
      };
    });
  }

  private formatHttpError(err: unknown): string {
    return formatJourneyHttpError(err);
  }

  /** Dev-only structured log for support (abort vs timeout vs HTTP). */
  private logJourneyHttpDev(scope: string, err: unknown): void {
    if (environment.production) return;
    if (err instanceof TimeoutError) {
      this.logger.debug(`[Journey HTTP] ${scope}`, { kind: 'timeout' });
      return;
    }
    if (err instanceof HttpErrorResponse) {
      this.logger.debug(`[Journey HTTP] ${scope}`, {
        kind: 'http',
        status: err.status,
        url: err.url ?? undefined,
      });
      return;
    }
    this.logger.debug(`[Journey HTTP] ${scope}`, { kind: 'http', detail: String(err) });
  }

  selectChapter(id: string): void {
    this.selectedChapterId.set(id);
    this.departmentFilter.set('all');
    this.oeQuickFilter.set('all');
    const snap = this._snapshot();
    const ch = snap?.chapters.find((c) => c.id === id);
    const firstWithOes = ch?.standards.find((s) => s.objectiveElements.length > 0);
    this.selectedStandardId.set(firstWithOes?.id ?? ch?.standards[0]?.id ?? null);
    this.selectedOeId.set(null);
    if (!environment.useJourneyMockWorkspace) {
      const force = this.maturityStageFilter() !== null;
      const needsLoad = force || !ch || ch.standards.length === 0;
      if (needsLoad) {
        this.ensureChapterStandardsLoaded(id, force);
      }
    }
  }

  selectStandard(standardId: string): void {
    const snap = this._snapshot();
    if (!snap) return;
    for (const ch of snap.chapters) {
      const st = ch.standards.find((s) => s.id === standardId);
      if (st) {
        this.selectedChapterId.set(ch.id);
        this.selectedStandardId.set(standardId);
        this.selectedOeId.set(null);
        return;
      }
    }
  }

  selectOe(standardId: string, oeId: string): void {
    this.selectStandard(standardId);
    this.selectedOeId.set(oeId);
    this.drawerOpen.set(true);
    if (!environment.useJourneyMockWorkspace) {
      this.refreshObjectiveElementDetail(oeId);
      if (this.oeWizardOpen()) {
        this.loadKnowledgeForSelectedOe();
      }
    }
  }

  closeDrawer(): void {
    this.drawerOpen.set(false);
  }

  setLifecycleMode(mode: OperationalMode): void {
    if (this.isModeLocked(mode)) return;
    const current = this.lifecycleMode();
    if (mode === current) return;
    const jid = this._snapshot()?.journeyId;
    if (!jid) return;

    if (environment.useJourneyMockWorkspace) {
      this.lifecycleMode.set(mode);
      const snap = this.mock.getWorkspace(jid, mode);
      this._snapshot.set(snap);
      return;
    }

    this.loading.set(true);
    this.journeyData
      .patchOperationalMode(jid, { operationalMode: mode })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (session) => {
          const lm = normOperationalMode(session.operationalMode);
          if (lm != null) {
            this.lifecycleMode.set(lm);
          }
          this.reloadAfterOperationalModeChange(jid);
        },
        error: (err: unknown) => {
          this.loading.set(false);
          this.notification.showError(this.formatHttpError(err));
        },
      });
  }

  private reloadAfterOperationalModeChange(journeyId: string): void {
    const modeQ = { operationalMode: this.lifecycleMode() };
    this.workspaceLoad$.next({
      journeyId,
      modeQuery: modeQ,
      resetChapterStandards: true,
    });
  }

  private refreshObjectiveElementDetail(oeId: string): void {
    const jid = this._snapshot()?.journeyId;
    if (!jid) return;
    this.journeyData
      .getObjectiveElement(jid, oeId, { operationalMode: this.lifecycleMode() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (oe) => {
          const found = this.findOe(oeId);
          if (!found) return;
          this.mergeOeIntoPlace(oe, found.chapter.id, found.standard.id);
        },
        error: () => {},
      });
  }

  /** After wizard evidence upload — re-fetch selected OE from Gateway. */
  refreshSelectedObjectiveElementFromApi(): void {
    const oeId = this.selectedOeId();
    if (!oeId) return;
    this.refreshObjectiveElementDetail(oeId);
  }

  private mergeOeIntoPlace(updated: ObjectiveElement, chapterId: string, standardId: string): void {
    this._snapshot.update((snap) => {
      if (!snap) return snap;
      const chapters = snap.chapters.map((ch) => {
        if (ch.id !== chapterId) return ch;
        return {
          ...ch,
          standards: ch.standards.map((st) => {
            if (st.id !== standardId) return st;
            return {
              ...st,
              objectiveElements: st.objectiveElements.map((o) => {
                if (o.id !== updated.id) {
                  return o;
                }
                const jws =
                  updated.journeyWorkflowStatus != null &&
                  String(updated.journeyWorkflowStatus).trim() !== ''
                    ? updated.journeyWorkflowStatus
                    : o.journeyWorkflowStatus;
                return { ...o, ...updated, standardId: st.id, journeyWorkflowStatus: jws };
              }),
            };
          }),
        };
      });
      return { ...snap, chapters };
    });
  }

  /** Multipart evidence upload + OE refresh (API mode). */
  uploadEvidenceFile(file: File, evidenceType = 'PolicyDocument'): void {
    const oeId = this.selectedOeId();
    if (!oeId) return;
    if (file.size === 0) {
      this.notification.showError('Empty files cannot be uploaded. Choose a file with content.');
      return;
    }
    if (file.size > 10 * 1024 * 1024) {
      this.notification.showError('File is too large (max 10 MB).');
      return;
    }
    if (environment.useJourneyMockWorkspace) {
      const item: OeEvidenceItem = {
        id: `ev-${Date.now()}`,
        name: file.name,
        verificationStatus: 'pending',
        uploadedAt: new Date().toISOString(),
        expiryAt: null,
        uploadedBy: 'Local',
      };
      this.appendLocalEvidence(oeId, item);
      this.notification.showSuccess('Evidence added (offline demo).');
      return;
    }
    const jid = this._snapshot()?.journeyId;
    if (!jid) return;
    this.journeyData
      .postEvidenceMultipart(jid, oeId, file, evidenceType, file.name)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.refreshObjectiveElementDetail(oeId);
          this.notification.showSuccess('Evidence uploaded.');
        },
        error: (err: unknown) => this.notification.showError(formatEvidenceHttpError(err)),
      });
  }

  setChapterSearch(q: string): void {
    this.chapterSearch.set(q);
  }

  toggleChapterRiskSort(): void {
    this.sortChaptersByRiskDesc.update((v) => !v);
  }

  /** Set chapter list sort: `true` = risk (high first), `false` = code order. */
  setChapterSortByRiskDesc(byRisk: boolean): void {
    this.sortChaptersByRiskDesc.set(byRisk);
  }

  setDepartmentFilter(id: string): void {
    this.departmentFilter.set(id);
  }

  setOeQuickFilter(filter: OeQuickFilter): void {
    this.oeQuickFilter.set(filter);
  }

  setMaturityStageFilter(stage: MaturityStageFilter): void {
    if (this.maturityStageFilter() === stage) return;
    this.maturityStageFilter.set(stage);

    const jid = this._snapshot()?.journeyId ?? this.journeyContext.resolveJourneyId();
    if (jid) {
      this.journeyCache.clearStandardsForJourney(jid);
    }
    this.standardsRequestByChapter.clear();

    if (!jid || environment.useJourneyMockWorkspace) {
      this.reconcileChapterSelectionAfterStageFilter();
      return;
    }

    // Keep the full chapter index; re-fetch standards for the active chapter with the new stage filter.
    this._snapshot.update((snap) => {
      if (!snap) return snap;
      return {
        ...snap,
        chapters: snap.chapters.map((c) => ({ ...c, standards: [] })),
      };
    });
    this.reconcileChapterSelectionAfterStageFilter();
  }

  /**
   * Seeds the stage filter from the URL before the first workspace load (avoids a
   * redundant unfiltered fetch when landing on /execution?stage=core).
   */
  seedMaturityStageFilter(stage: MaturityStageFilter): void {
    if (stage && this.maturityStageFilter() !== stage) {
      this.maturityStageFilter.set(stage);
    }
  }

  /** Keeps chapter/standard selection valid after stage filter changes. */
  private reconcileChapterSelectionAfterStageFilter(): void {
    const visible = this.filteredChapters();
    const current = this.selectedChapterId();
    if (visible.length === 0) {
      this.selectedChapterId.set(null);
      this.selectedStandardId.set(null);
      this.selectedOeId.set(null);
      return;
    }
    if (!current || !visible.some((c) => c.id === current)) {
      const first = visible[0];
      this.selectedChapterId.set(first.id);
      this.selectedStandardId.set(null);
      this.selectedOeId.set(null);
      if (!environment.useJourneyMockWorkspace) {
        this.ensureChapterStandardsLoaded(first.id, true);
      }
      return;
    }
    if (!environment.useJourneyMockWorkspace) {
      this.ensureChapterStandardsLoaded(current, true);
    }
  }

  /**
   * Select OE for execution context (right panel) without opening the OE detail drawer.
   */
  selectExecutionContext(standardId: string, oeId: string): void {
    this.selectStandard(standardId);
    this.selectedOeId.set(oeId);
    this.drawerOpen.set(false);
    if (!environment.useJourneyMockWorkspace) {
      this.refreshObjectiveElementDetail(oeId);
      if (this.oeWizardOpen()) {
        this.loadKnowledgeForSelectedOe();
      }
    }
  }

  filteredOesForStandard(std: ComplianceStandard): ObjectiveElement[] {
    const mode = this.lifecycleMode();
    let oes = sortOesByCode(filterOesByLifecycle(std.objectiveElements, mode));
    const df = this.departmentFilter();
    if (df !== 'all') {
      oes = oes.filter((oe) => this.oeMatchesDepartmentFilter(oe.departmentLabel, df));
    }
    const qf = this.oeQuickFilter();
    if (qf !== 'all') {
      oes = oes.filter((oe) => this.oeMatchesQuickFilter(oe, qf));
    }
    // API mode: stage narrowing is applied via classificationFilter on GET …/standards.
    // Re-filtering here hid OEs when the wire format differed from the active stage key.
    if (environment.useJourneyMockWorkspace) {
      const stageFilter = this.maturityStageFilter();
      if (stageFilter) {
        oes = oes.filter((oe) => (oe.classification ?? '').toLowerCase() === stageFilter);
      }
    }
    return oes;
  }

  /** Computed alias for template/store consumers wanting reactive access to stage-filtered OEs. */
  readonly filteredOesByStage = computed((): ObjectiveElement[] => {
    const snap = this._snapshot();
    if (!snap) return [];
    const stageFilter = this.maturityStageFilter();
    if (!stageFilter) return [];
    const all: ObjectiveElement[] = [];
    for (const ch of snap.chapters) {
      for (const st of ch.standards) {
        for (const oe of st.objectiveElements) {
          if ((oe.classification ?? '').toLowerCase() === stageFilter) {
            all.push(oe);
          }
        }
      }
    }
    return all;
  });

  private oeMatchesQuickFilter(oe: ObjectiveElement, f: OeQuickFilter): boolean {
    if (f === 'core_only') {
      return (oe.classification ?? '').toLowerCase() === 'core';
    }
    if (f === 'non_compliant') {
      return oe.auditStatus === 'non_compliant';
    }
    if (f === 'high_risk') {
      return (
        oe.score <= 2 ||
        oe.auditStatus === 'non_compliant' ||
        (oe.auditStatus === 'compliant' && oe.score <= 3)
      );
    }
    return true;
  }

  private oeMatchesDepartmentFilter(label: string | undefined, filterKey: string): boolean {
    if (!label?.trim()) return false;
    const l = label.trim().toLowerCase();
    const keys: Record<string, string[]> = {
      clinical: ['clinical', 'patient care', 'nursing'],
      pharmacy: ['pharmacy', 'pharm'],
      hr: ['hr', 'human resource'],
      facility: ['facility', 'facilities', 'engineering', 'estates'],
      infection_control: ['infection', 'ipc', 'infection control'],
    };
    const words = keys[filterKey];
    if (!words) return true;
    return words.some((w) => l.includes(w));
  }

  /** Immutable local patch of one OE (no HTTP). */
  patchObjectiveElement(
    chapterId: string,
    standardId: string,
    oeId: string,
    patch: Partial<ObjectiveElement>,
  ): void {
    this._snapshot.update((snap) => {
      if (!snap) return snap;
      const chapters = snap.chapters.map((ch) => {
        if (ch.id !== chapterId) return ch;
        return {
          ...ch,
          standards: ch.standards.map((st) => {
            if (st.id !== standardId) return st;
            return {
              ...st,
              objectiveElements: st.objectiveElements.map((oe) =>
                oe.id === oeId ? { ...oe, ...patch } : oe,
              ),
            };
          }),
        };
      });
      return { ...snap, chapters };
    });
  }

  /**
   * Apply score / N/A / remarks — mock: local; API: PATCH OE with optimistic concurrency.
   */
  applyLocalObjectiveElementPatch(
    chapterId: string,
    standardId: string,
    oeId: string,
    body: PatchObjectiveElementBody,
  ): void {
    if (environment.useJourneyMockWorkspace) {
      this.applyLocalObjectiveElementPatchMock(chapterId, standardId, oeId, body);
      return;
    }
    const found = this.findOe(oeId);
    if (!found) return;
    const jid = this._snapshot()?.journeyId;
    if (!jid) return;

    const req: PatchObjectiveElementBody = { ...body };
    if (found.oe.version != null) {
      req.version = found.oe.version;
    }

    this.journeyData
      .patchObjectiveElement(jid, oeId, req, { operationalMode: this.lifecycleMode() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => this.applyPatchResponse(res, chapterId, standardId, oeId),
        error: (err: unknown) => {
          if (err instanceof HttpErrorResponse && err.status === 409) {
            this.refreshObjectiveElementDetail(oeId);
            this.notification.showWarning(
              'This objective element was updated elsewhere. Refreshed from server.',
            );
          } else {
            this.notification.showError(this.formatHttpError(err));
          }
        },
      });
  }

  private applyPatchResponse(
    res: PatchObjectiveElementResponseDto,
    chapterId: string,
    standardId: string,
    oeId: string,
  ): void {
    this._snapshot.update((snap) => {
      if (!snap) return snap;
      let next: JourneyWorkspaceSnapshot = snap;
      if (res.overview) {
        next = {
          ...next,
          overview: mapOverviewMetricsApiToDomain(res.overview),
        };
      }
      const frag = res.objectiveElement;
      if (!frag?.id) return next;
      const mapped = mapObjectiveElementApiToDomain(frag);
      const chapters = next.chapters.map((ch) => {
        if (ch.id !== chapterId) return ch;
        return {
          ...ch,
          standards: ch.standards.map((st) => {
            if (st.id !== standardId) return st;
            return {
              ...st,
              objectiveElements: st.objectiveElements.map((o) => {
                if (o.id !== oeId) {
                  return o;
                }
                const jws =
                  mapped.journeyWorkflowStatus != null && String(mapped.journeyWorkflowStatus).trim() !== ''
                    ? mapped.journeyWorkflowStatus
                    : o.journeyWorkflowStatus;
                return { ...o, ...mapped, id: o.id, standardId: st.id, journeyWorkflowStatus: jws };
              }),
            };
          }),
        };
      });
      return { ...next, chapters };
    });
  }

  private applyLocalObjectiveElementPatchMock(
    chapterId: string,
    standardId: string,
    oeId: string,
    body: PatchObjectiveElementBody,
  ): void {
    const found = this.findOe(oeId);
    if (!found) return;

    const optimistic: Partial<ObjectiveElement> = {};
    if (body.isNa !== undefined) optimistic.isNa = body.isNa;
    if (body.naJustification !== undefined) {
      optimistic.naJustification = body.naJustification ?? undefined;
    }
    if (body.score !== undefined) {
      const r = Math.round(Number(body.score));
      optimistic.score = (Math.min(5, Math.max(1, r)) || 3) as NabhScore;
    }
    if (body.scoreRationale !== undefined) optimistic.scoreRationale = body.scoreRationale ?? undefined;
    if (body.auditRemarks !== undefined) optimistic.auditRemarks = body.auditRemarks ?? undefined;

    this.patchObjectiveElement(chapterId, standardId, oeId, optimistic);
  }

  /** @deprecated Use {@link applyLocalObjectiveElementPatch}. */
  submitObjectiveElementPatch(
    chapterId: string,
    standardId: string,
    oeId: string,
    body: PatchObjectiveElementBody,
  ): void {
    this.applyLocalObjectiveElementPatch(chapterId, standardId, oeId, body);
  }

  replaceObjectiveElementEvidence(oeId: string, evidence: OeEvidenceItem[]): void {
    this._snapshot.update((snap) => {
      if (!snap) return snap;
      const chapters = snap.chapters.map((ch) => ({
        ...ch,
        standards: ch.standards.map((st) => ({
          ...st,
          objectiveElements: st.objectiveElements.map((oe) =>
            oe.id === oeId ? { ...oe, evidence } : oe,
          ),
        })),
      }));
      return { ...snap, chapters };
    });
  }

  /**
   * OE list rows read `journeyWorkflowStatus` from the chapter snapshot. Wizard GET/PUT/POST return
   * `ObjectiveJourneyStateDto.status` but do not re-fetch chapter standards — sync the field here.
   */
  private patchObjectiveElementJourneyWorkflow(
    oeId: string,
    journeyWorkflowStatus: 'not_started' | 'in_progress' | 'completed',
  ): void {
    this._snapshot.update((snap) => {
      if (!snap) return snap;
      const chapters = snap.chapters.map((ch) => ({
        ...ch,
        standards: ch.standards.map((st) => ({
          ...st,
          objectiveElements: st.objectiveElements.map((oe) =>
            oe.id === oeId ? { ...oe, journeyWorkflowStatus } : oe,
          ),
        })),
      }));
      return { ...snap, chapters };
    });
    this.syncChapterStandardsCacheForOe(oeId);
  }

  private syncChapterStandardsCacheForOe(oeId: string): void {
    const snap = this._snapshot();
    if (!snap) {
      return;
    }
    const found = this.findOe(oeId);
    if (!found) {
      return;
    }
    this.journeyCache.setChapterStandards(
      snap.journeyId,
      found.chapter.id,
      this.lifecycleMode(),
      found.chapter.standards,
      this.maturityStageFilter(),
    );
  }

  appendLocalEvidence(oeId: string, item: OeEvidenceItem): void {
    const found = this.findOe(oeId);
    if (!found) return;
    const next = [...found.oe.evidence, item];
    this.replaceObjectiveElementEvidence(oeId, next);
  }

  private setOeMutating(oeId: string, active: boolean): void {
    this.oeMutationInFlight.update((prev) => {
      const next = new Set(prev);
      if (active) next.add(oeId);
      else next.delete(oeId);
      return next;
    });
  }

  registerOeMutation(oeId: string, active: boolean): void {
    this.setOeMutating(oeId, active);
  }

  isOeMutating(oeId: string): boolean {
    return this.oeMutationInFlight().has(oeId);
  }

  findOe(oeId: string): { chapter: JourneyChapter; standard: ComplianceStandard; oe: ObjectiveElement } | null {
    const snap = this._snapshot();
    if (!snap) return null;
    for (const ch of snap.chapters) {
      for (const st of ch.standards) {
        const oe = st.objectiveElements.find((o) => o.id === oeId);
        if (oe) return { chapter: ch, standard: st, oe };
      }
    }
    return null;
  }

  isModeLocked(mode: OperationalMode): boolean {
    const current = this.lifecycleMode();
    if (mode === current) return false;
    if (!environment.useJourneyMockWorkspace) {
      const snap = this._snapshot();
      const allowed = snap?.modeProgression?.allowedNextModes;
      if (allowed && allowed.length > 0) {
        return !allowed.includes(mode);
      }
    }
    if (this.readOnly() && current !== mode) return true;
    const ov = this.overview();
    return operationalModeProgressionBlocked(
      mode,
      current,
      ov ? { coreCompliancePercent: ov.coreCompliancePercent, failedCoreCount: ov.failedCoreCount } : null,
    ).blocked;
  }

  /** Why `mode` cannot be selected (null if current mode or available). */
  modeLockReason(mode: OperationalMode): string | null {
    const current = this.lifecycleMode();
    if (mode === current) return null;
    if (!environment.useJourneyMockWorkspace) {
      const snap = this._snapshot();
      const fromApi = snap?.modeProgression?.lockReasonByTargetMode?.[String(mode)];
      if (fromApi != null && String(fromApi).trim()) {
        return String(fromApi);
      }
      const allowed = snap?.modeProgression?.allowedNextModes;
      if (allowed && allowed.length > 0 && !allowed.includes(mode)) {
        return 'Next mode is not available yet.';
      }
    }
    if (this.readOnly() && current !== mode) {
      return 'Mode switching is disabled while the organization is in a read-only audit phase.';
    }
    const ov = this.overview();
    return operationalModeProgressionBlocked(
      mode,
      current,
      ov ? { coreCompliancePercent: ov.coreCompliancePercent, failedCoreCount: ov.failedCoreCount } : null,
    ).reason;
  }

  highestRiskStandardTarget(): { chapterId: string; standardId: string } | null {
    const snap = this._snapshot();
    if (!snap) return null;

    const chapterOrder: Record<JourneyChapter['risk'], number> = { high: 0, medium: 1, low: 2 };
    const sorted = [...snap.chapters].sort(
      (a, b) => chapterOrder[a.risk] - chapterOrder[b.risk] || a.code.localeCompare(b.code),
    );
    const chapter = sorted[0];
    const standard = chapter?.standards[0];
    if (!chapter || !standard) return null;
    return { chapterId: chapter.id, standardId: standard.id };
  }

  // --- OE full-screen wizard (Phase 44) ---

  /**
   * Deep-link `/execution/Progress/:oeGuid`: select OE when workspace is ready.
   * Requests chapter standards until the OE can be resolved or all chapters are loaded.
   */
  tryApplyRouteObjectiveElement(oeId: string): 'applied' | 'loading' | 'not_found' {
    if (this.loading()) return 'loading';
    if (this.standardsLoadingChapterId()) return 'loading';
    const found = this.findOe(oeId);
    if (found) {
      const alreadySelected =
        this.selectedOeId() === oeId && this.selectedStandardId() === found.standard.id;
      if (!alreadySelected) {
        this.selectExecutionContext(found.standard.id, oeId);
      }
      return 'applied';
    }
    const snap = this._snapshot();
    if (!snap) return 'not_found';
    for (const ch of snap.chapters) {
      if (ch.standards.length === 0) {
        this.ensureChapterStandardsLoaded(ch.id);
        return 'loading';
      }
    }
    return 'not_found';
  }

  openOeWizard(): void {
    if (!this.selectedOeId()) return;
    if (!environment.useJourneyMockWorkspace) {
      this.refreshObjectiveElementDetail(this.selectedOeId()!);
      this.loadKnowledgeForSelectedOe();
    } else {
      this.clearKnowledgeBaseState();
    }

    const jid = this._snapshot()?.journeyId;
    const oeId = this.selectedOeId();
    if (!environment.useJourneyMockWorkspace && jid && oeId) {
      this.journeyData
        .getOeExecutionWizard(jid, oeId)
        .pipe(
          catchError((err: unknown) => {
            const http = err as HttpErrorResponse;
            if (http.status === 404) {
              this.restoreWizardFromStorage();
              this.oeWizardOpen.set(true);
              return EMPTY;
            }
            if (http.status === 423) {
              // Stage is locked — prerequisite threshold not met. Show guidance and do not open wizard.
              this.notification.showError(
                'This maturity stage is locked. Complete the prerequisite stage first to unlock access.',
              );
              return EMPTY;
            }
            this.logJourneyHttpDev('oe-wizard-get', err);
            this.notification.showError(formatJourneyHttpError(err));
            this.restoreWizardFromStorage();
            this.oeWizardOpen.set(true);
            return EMPTY;
          }),
          takeUntilDestroyed(this.destroyRef),
        )
        .subscribe((dto) => {
          this.applyWizardFromServer(dto);
          this.oeWizardOpen.set(true);
        });
      return;
    }

    this.restoreWizardFromStorage();
    this.oeWizardOpen.set(true);
  }

  /** Loads KB content for the current OE (`nodeCode` = `ObjectiveElement.code`); see KB wizard binding doc. */
  loadKnowledgeForSelectedOe(): void {
    const gen = ++this.knowledgeLoadGeneration;
    this.knowledgeBaseNodeDetails.set(null);
    this.knowledgeBaseError.set(null);

    if (environment.useJourneyMockWorkspace) {
      this.knowledgeBaseLoading.set(false);
      return;
    }

    const oeId = this.selectedOeId();
    if (!oeId) {
      this.knowledgeBaseLoading.set(false);
      return;
    }
    const found = this.findOe(oeId);
    if (!found) {
      this.knowledgeBaseLoading.set(false);
      return;
    }

    this.knowledgeBaseLoading.set(true);

    this.kb
      .resolveNodeDetailsForObjectiveElement(found.oe)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (dto) => {
          if (gen !== this.knowledgeLoadGeneration) return;
          this.knowledgeBaseNodeDetails.set(dto);
          this.knowledgeBaseLoading.set(false);
          this.knowledgeBaseError.set(null);
          this.reconcileWizardAuditChecklistFromKb();
        },
        error: (err: unknown) => {
          if (gen !== this.knowledgeLoadGeneration) return;
          this.knowledgeBaseLoading.set(false);
          this.knowledgeBaseNodeDetails.set(null);
          this.knowledgeBaseError.set(this.formatHttpError(err));
        },
      });
  }

  private clearKnowledgeBaseState(): void {
    this.knowledgeLoadGeneration++;
    this.knowledgeBaseLoading.set(false);
    this.knowledgeBaseError.set(null);
    this.knowledgeBaseNodeDetails.set(null);
  }

  private static reconcileBooleanArrayToLength(arr: boolean[], length: number): boolean[] {
    if (length <= 0) return [];
    const next = arr.slice(0, length);
    while (next.length < length) next.push(false);
    return next;
  }

  private parsePersistedAuditChecklist(raw: unknown): boolean[] {
    if (!Array.isArray(raw)) return [];
    return raw.map((x) => x === true);
  }

  /** When KB `auditChecklist` text or line count changes, pad/truncate persisted booleans. */
  private reconcileWizardAuditChecklistFromKb(): void {
    if (environment.useJourneyMockWorkspace) return;
    const oid = this.selectedOeId();
    if (!oid) return;
    const found = this.findOe(oid);
    if (!found) return;
    if (this.knowledgeBaseLoading()) return;
    if (this.knowledgeBaseError() && !this.knowledgeBaseNodeDetails()) return;

    const kb = mapNodeDetailsToWizardVm(this.knowledgeBaseNodeDetails(), found.oe);
    const raw = kb?.auditChecklist ?? '';
    const content = raw.trim();
    if (content === this.wizardAuditChecklistContentKey) return;

    this.wizardAuditChecklistContentKey = content;
    const lines = splitAuditChecklistLines(raw);
    const next = JourneyWorkspaceStore.reconcileBooleanArrayToLength(
      this.wizardAuditChecklistChecked(),
      lines.length,
    );
    this.wizardAuditChecklistChecked.set(next);
    if (lines.length > 0) {
      const checked = next;
      const total = checked.length;
      const done = checked.filter(Boolean).length;
      const score = Math.max(1, Math.min(5, Math.round(1 + (done / total) * 4)));
      this.wizardEvalScore.set(score);
    }
    this.saveOeWizardToStorage();
  }

  /** Toggle audit checklist line; updates local 1–5 score from completion ratio. */
  setWizardAuditLineChecked(index: number, checked: boolean): void {
    if (this.wizardInputsLocked()) return;
    this.wizardAuditChecklistChecked.update((arr) => {
      const copy = [...arr];
      if (index >= 0 && index < copy.length) copy[index] = checked;
      return copy;
    });
    const arr = this.wizardAuditChecklistChecked();
    if (arr.length > 0) {
      const done = arr.filter(Boolean).length;
      const score = Math.max(1, Math.min(5, Math.round(1 + (done / arr.length) * 4)));
      this.wizardEvalScore.set(score);
    }
    this.markWizardDirty();
  }

  private wizardStorageKey(): string {
    const uid = this.session.getUserId() ?? 'anonymous';
    const jid = this._snapshot()?.journeyId ?? '';
    const oid = this.selectedOeId() ?? '';
    return `journey.wizard.${uid}.${jid}.${oid}`;
  }

  buildWizardPayloadObject(): OeWizardPersistedState {
    return {
      stepIndex: this.oeWizardStepIndex(),
      understandAck: this.wizardUnderstandAck(),
      executeReady: this.wizardExecuteReady(),
      evidenceAck: this.wizardEvidenceAck(),
      evalSubmitted: this.wizardEvalSubmitted(),
      evalScore: this.wizardEvalScore(),
      evalTone: this.wizardEvalTone(),
      evalJustification: this.wizardEvalJustification(),
      needsImprove: this.wizardNeedsImprove(),
      improveReady: this.wizardImproveReady(),
      completeAck: this.wizardCompleteAck(),
      auditChecklistChecked: [...this.wizardAuditChecklistChecked()],
      savedAt: new Date().toISOString(),
    };
  }

  private applyWizardFromServer(dto: ObjectiveJourneyStateDto): void {
    const s = dto.payload;
    this.oeWizardStepIndex.set(typeof s.stepIndex === 'number' ? s.stepIndex : 0);
    this.wizardUnderstandAck.set(!!s.understandAck);
    this.wizardExecuteReady.set(!!s.executeReady);
    this.wizardEvidenceAck.set(typeof s.evidenceAck === 'boolean' ? s.evidenceAck : false);
    this.wizardEvalSubmitted.set(!!s.evalSubmitted);
    this.wizardEvalScore.set(
      typeof s.evalScore === 'number' ? Math.min(5, Math.max(1, s.evalScore)) : 3,
    );
    this.wizardEvalTone.set((s.evalTone as 'compliant' | 'partial' | 'non') ?? 'compliant');
    this.wizardEvalJustification.set(typeof s.evalJustification === 'string' ? s.evalJustification : '');
    this.wizardEvalJustificationError.set(false);
    this.wizardNeedsImprove.set(!!s.needsImprove);
    this.wizardImproveReady.set(!!s.improveReady);
    this.wizardCompleteAck.set(!!s.completeAck);
    this.wizardAuditChecklistChecked.set(this.parsePersistedAuditChecklist(s.auditChecklistChecked));
    this.wizardAuditChecklistContentKey = '';
    this.wizardRowVersion.set(dto.rowVersion);
    this.oeWizardRemoteCompleted.set(isObjectiveJourneyStatusCompleted(dto.status));
    this.lastWizardCanonSent.set(canonicalizeOeWizardPayload(dto.payload));
    this.oeWizardDirty.set(false);
    this.saveOeWizardToStorage();
    this.reconcileWizardAuditChecklistFromKb();
    this.patchObjectiveElementJourneyWorkflow(
      dto.objectiveElementId,
      objectiveJourneyWizardStatusToJourneyWorkflowStatus(dto.status),
    );
  }

  private reloadWizardFromServer(): void {
    const jid = this._snapshot()?.journeyId;
    const oid = this.selectedOeId();
    if (!jid || !oid || environment.useJourneyMockWorkspace) return;
    this.journeyData
      .getOeExecutionWizard(jid, oid)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (dto) => this.applyWizardFromServer(dto),
        error: () => {
          /* ignore */
        },
      });
  }

  /** Save draft immediately (same PUT as autosave; bypasses throttle). */
  flushWizardDraftToServer(): void {
    if (this.oeWizardRemoteCompleted()) return;
    const jid = this._snapshot()?.journeyId;
    const oid = this.selectedOeId();
    if (!jid || !oid || environment.useJourneyMockWorkspace) {
      this.markWizardDirty();
      return;
    }
    const payload = this.buildWizardPayloadObject();
    this.journeyData
      .putOeExecutionWizardDraft(jid, oid, {
        payload,
        rowVersion: this.wizardRowVersion() ?? undefined,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (dto) => {
          this.wizardRowVersion.set(dto.rowVersion);
          this.oeWizardRemoteCompleted.set(isObjectiveJourneyStatusCompleted(dto.status));
          this.lastWizardCanonSent.set(canonicalizeOeWizardPayload(dto.payload));
          const wid = this.selectedOeId();
          if (wid) {
            this.patchObjectiveElementJourneyWorkflow(
              wid,
              objectiveJourneyWizardStatusToJourneyWorkflowStatus(dto.status),
            );
          }
        },
        error: (err: unknown) => {
          const http = err as HttpErrorResponse;
          if (http.status === 409) {
            this.notification.showError('Wizard was updated in another session. Reloading saved state.');
            this.reloadWizardFromServer();
          } else {
            this.logJourneyHttpDev('oe-wizard-draft-flush', err);
            this.notification.showError(formatJourneyHttpError(err));
          }
        },
      });
  }

  /** Mark complete locally and finalize on server (maps to Patch OE + optional task completion). */
  completeOeWizardOnServer(): void {
    const jid = this._snapshot()?.journeyId;
    const oid = this.selectedOeId();
    if (!jid || !oid || environment.useJourneyMockWorkspace) {
      this.wizardCompleteAck.set(true);
      this.saveOeWizardToStorage();
      return;
    }

    const found = this.findOe(oid);
    const payloadPreview = this.buildWizardPayloadObject();
    if (payloadPreview.evalScore === 5 && found && !oeHasVerifiedEvidence(found.oe)) {
      this.notification.showError(
        'A score of 5 requires at least one verified evidence item for this objective. Verify evidence in the workspace or lower the evaluation score.',
      );
      return;
    }

    this.wizardCompleteAck.set(true);
    this.saveOeWizardToStorage();
    const payload = this.buildWizardPayloadObject();
    const body: CompleteObjectiveJourneyRequest = {
      payload,
      rowVersion: this.wizardRowVersion() ?? undefined,
    };
    this.journeyData
      .postOeExecutionWizardComplete(jid, oid, body, { operationalMode: this.lifecycleMode() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (dto) => {
          this.applyWizardFromServer(dto);
          this.refreshObjectiveElementDetail(oid);
          this.notification.showSuccess('Execution wizard completed.');
        },
        error: (err: unknown) => {
          const http = err as HttpErrorResponse;
          if (http.status === 409) {
            this.notification.showError('Could not complete: version conflict. Reloading state.');
            this.reloadWizardFromServer();
          } else {
            this.logJourneyHttpDev('oe-wizard-complete', err);
            this.notification.showError(formatJourneyHttpError(err));
          }
        },
      });
  }

  private restoreWizardFromStorage(): void {
    const raw = sessionStorage.getItem(this.wizardStorageKey());
    if (!raw) {
      this.resetOeWizardState();
      return;
    }
    try {
      const s = JSON.parse(raw) as OeWizardPersistedState;
      this.oeWizardStepIndex.set(typeof s.stepIndex === 'number' ? s.stepIndex : 0);
      this.wizardUnderstandAck.set(!!s.understandAck);
      this.wizardExecuteReady.set(!!s.executeReady);
      this.wizardEvidenceAck.set(
        typeof (s as OeWizardPersistedState).evidenceAck === 'boolean'
          ? (s as OeWizardPersistedState).evidenceAck
          : false,
      );
      this.wizardEvalSubmitted.set(!!s.evalSubmitted);
      this.wizardEvalScore.set(
        typeof s.evalScore === 'number' ? Math.min(5, Math.max(1, s.evalScore)) : 3,
      );
      this.wizardEvalTone.set(s.evalTone ?? 'compliant');
      this.wizardEvalJustification.set(typeof s.evalJustification === 'string' ? s.evalJustification : '');
      this.wizardEvalJustificationError.set(false);
      this.wizardNeedsImprove.set(!!s.needsImprove);
      this.wizardImproveReady.set(!!s.improveReady);
      this.wizardCompleteAck.set(!!s.completeAck);
      this.wizardAuditChecklistChecked.set(this.parsePersistedAuditChecklist(s.auditChecklistChecked));
      this.wizardAuditChecklistContentKey = '';
      this.oeWizardDirty.set(false);
      this.wizardRowVersion.set(null);
      this.oeWizardRemoteCompleted.set(false);
      this.lastWizardCanonSent.set(canonicalizeOeWizardPayload(s));
    } catch {
      this.resetOeWizardState();
      return;
    }
    this.reconcileWizardAuditChecklistFromKb();
  }

  private resetOeWizardState(): void {
    this.wizardRowVersion.set(null);
    this.oeWizardRemoteCompleted.set(false);
    this.lastWizardCanonSent.set('');
    this.oeWizardStepIndex.set(0);
    this.oeWizardDirty.set(false);
    this.wizardUnderstandAck.set(false);
    this.wizardExecuteReady.set(false);
    this.wizardEvidenceAck.set(false);
    this.wizardEvalSubmitted.set(false);
    this.wizardEvalScore.set(3);
    this.wizardEvalTone.set('compliant');
    this.wizardEvalJustification.set('');
    this.wizardEvalJustificationError.set(false);
    this.wizardNeedsImprove.set(false);
    this.wizardImproveReady.set(false);
    this.wizardCompleteAck.set(false);
    this.wizardAuditChecklistChecked.set([]);
    this.wizardAuditChecklistContentKey = '';
  }

  saveOeWizardToStorage(): void {
    if (!this.selectedOeId()) return;
    const state: OeWizardPersistedState = {
      stepIndex: this.oeWizardStepIndex(),
      understandAck: this.wizardUnderstandAck(),
      executeReady: this.wizardExecuteReady(),
      evidenceAck: this.wizardEvidenceAck(),
      evalSubmitted: this.wizardEvalSubmitted(),
      evalScore: this.wizardEvalScore(),
      evalTone: this.wizardEvalTone(),
      evalJustification: this.wizardEvalJustification(),
      needsImprove: this.wizardNeedsImprove(),
      improveReady: this.wizardImproveReady(),
      completeAck: this.wizardCompleteAck(),
      auditChecklistChecked: [...this.wizardAuditChecklistChecked()],
      savedAt: new Date().toISOString(),
    };
    sessionStorage.setItem(this.wizardStorageKey(), JSON.stringify(state));
  }

  clearOeWizardStorage(): void {
    sessionStorage.removeItem(this.wizardStorageKey());
  }

  closeOeWizardWithoutConfirm(): void {
    this.saveOeWizardToStorage();
    this.clearKnowledgeBaseState();
    this.taskAssignmentDrawerOpen.set(false);
    this.oeWizardOpen.set(false);
  }

  dismissOeWizardDirty(): void {
    this.oeWizardDirty.set(false);
    this.clearOeWizardStorage();
    this.resetOeWizardState();
    this.clearKnowledgeBaseState();
    this.taskAssignmentDrawerOpen.set(false);
    this.oeWizardOpen.set(false);
  }

  setWizardStep(step: number): void {
    if (step < 0 || step >= OE_WIZARD_STEP_LABELS.length) return;
    if (!this.oeWizardCanAccessStep()(step)) return;
    this.oeWizardStepIndex.set(step);
    if (!this.oeWizardRemoteCompleted()) {
      this.markWizardDirty();
    }
  }

  markWizardDirty(): void {
    if (this.oeWizardRemoteCompleted()) return;
    this.oeWizardDirty.set(true);
    this.saveOeWizardToStorage();
    const jid = this._snapshot()?.journeyId;
    const oid = this.selectedOeId();
    if (!environment.useJourneyMockWorkspace && jid && oid && this.oeWizardOpen()) {
      this.wizardPersistTrigger$.next();
    }
  }

  wizardFinishUnderstand(): void {
    this.wizardUnderstandAck.set(true);
    this.setWizardStep(1);
  }

  wizardMarkExecuteReady(): void {
    this.wizardExecuteReady.set(true);
    this.markWizardDirty();
  }

  /** Advance from Compliance Guidance → Evidence. */
  wizardProceedToEvidence(): void {
    this.setWizardStep(2);
  }

  /** Mark Evidence step complete → Audit Checklist. */
  wizardCompleteEvidenceStep(): void {
    this.wizardEvidenceAck.set(true);
    this.setWizardStep(3);
  }

  setWizardEvalScore(score: number | string): void {
    const n = typeof score === 'string' ? Number(score) : score;
    const r = Math.round(Math.min(5, Math.max(1, Number.isFinite(n) ? n : 3)));
    this.wizardEvalScore.set(r);
    this.markWizardDirty();
  }

  setWizardEvalTone(tone: 'compliant' | 'partial' | 'non'): void {
    this.wizardEvalTone.set(tone);
    this.wizardEvalJustificationError.set(false);
    this.markWizardDirty();
  }

  setWizardEvalJustification(text: string): void {
    this.wizardEvalJustification.set(text);
    this.wizardEvalJustificationError.set(false);
    this.markWizardDirty();
  }

  wizardSubmitEvaluation(): void {
    const tone = this.wizardEvalTone();
    if ((tone === 'partial' || tone === 'non') && !this.wizardEvalJustification().trim()) {
      this.wizardEvalJustificationError.set(true);
      return;
    }
    this.wizardEvalJustificationError.set(false);
    this.wizardEvalSubmitted.set(true);
    this.wizardNeedsImprove.set(tone === 'partial' || tone === 'non');
    if (tone === 'compliant') {
      this.wizardImproveReady.set(true);
      this.oeWizardStepIndex.set(4);
    } else {
      this.oeWizardStepIndex.set(3);
    }
    this.markWizardDirty();
  }

  wizardMarkImproveReady(): void {
    this.wizardImproveReady.set(true);
    this.markWizardDirty();
  }

  wizardSetImproveReady(value: boolean): void {
    this.wizardImproveReady.set(value);
    this.markWizardDirty();
  }

  /** From Audit Checklist when improvement items require acknowledgment. */
  wizardProceedToReviewFromAudit(): void {
    if (this.wizardNeedsImprove() && !this.wizardImproveReady()) return;
    this.setWizardStep(4);
  }

  wizardProceedToCompleteFromImprove(): void {
    this.wizardProceedToReviewFromAudit();
  }

  wizardMarkComplete(): void {
    this.wizardCompleteAck.set(true);
    this.markWizardDirty();
  }

  wizardExitAfterFinish(): void {
    this.clearOeWizardStorage();
    this.resetOeWizardState();
    this.clearKnowledgeBaseState();
    this.taskAssignmentDrawerOpen.set(false);
    this.oeWizardOpen.set(false);
  }

  // --- Create & Assign Task drawer (Phase 59) ---

  openTaskAssignmentDrawer(): void {
    const snap = this._snapshot();
    const oeId = this.selectedOeId();
    const found = oeId ? this.findOe(oeId) : null;
    const contextual = buildTaskDraftFromOeContext(found, snap);
    const key = this.taskDraftStorageKey();
    let merged: CreateTaskDraftVm = { ...contextual };
    if (key) {
      const raw = sessionStorage.getItem(key);
      if (raw) {
        try {
          const saved = JSON.parse(raw) as Partial<CreateTaskDraftVm> & { assigneeIds?: string[] };
          merged = mergeTaskDraftFromSession(contextual, saved);
        } catch {
          /* ignore corrupt draft */
        }
      }
    }
    this._taskAssignmentDraft.set(merged);
    this.taskAssignmentDrawerOpen.set(true);
  }

  closeTaskAssignmentDrawer(): void {
    this.taskAssignmentDrawerOpen.set(false);
  }

  patchTaskAssignmentDraft(partial: Partial<CreateTaskDraftVm>): void {
    this._taskAssignmentDraft.update((d) => ({ ...d, ...partial }));
  }

  setTaskAssignmentDraft(next: CreateTaskDraftVm): void {
    this._taskAssignmentDraft.set(next);
  }

  resetTaskAssignmentDraft(): void {
    this._taskAssignmentDraft.set(createEmptyCreateTaskDraftVm());
  }

  /** Persist current draft to sessionStorage (namespaced by user + journey + OE). */
  saveTaskAssignmentDraftToSession(): void {
    const key = this.taskDraftStorageKey();
    if (!key) return;
    try {
      sessionStorage.setItem(key, JSON.stringify(this._taskAssignmentDraft()));
    } catch {
      /* quota */
    }
  }

  private taskDraftStorageKey(): string | null {
    const uid = this.session.getUserId() ?? 'anonymous';
    const jid = this._snapshot()?.journeyId ?? '';
    const oid = this.selectedOeId() ?? '';
    if (!jid || !oid) return null;
    return `praman.task-draft.${uid}.${jid}.${oid}`;
  }
}
