import { Injectable, computed, inject, signal } from '@angular/core';
import type { AuditWorkspaceTabId } from '../models/audit-workspace-tab.model';
import type {
  AuditWorkflowStage,
  AuditWorkflowStageProgress,
} from '../models/audit-workflow.model';
import { AuditWorkflowEngine } from './audit-workflow.engine';

const LAST_TAB_KEY = 'aovias.audit.workspace.lastTab.v1';
const CONTEXT_KEY = 'aovias.audit.workspace.context.v1';

/**
 * Workspace selection context — the "where am I working" of the workspace.
 *
 * Everything here is an id or a primitive, never a loaded entity. Pages own
 * their own data fetching; the workspace only remembers what was selected so a
 * tab switch resumes instead of resetting. Keeping it to ids also makes the
 * whole thing trivially serialisable for {@link CONTEXT_KEY}.
 */
export interface AuditWorkspaceContext {
  /** Scheduled audit the user is working through (planning → execution link). */
  scheduledAuditId: string | null;
  /** Execution session for that audit. One audit → one session. */
  sessionId: string | null;
  departmentId: string | null;
  /** Objective element / clause currently under assessment. */
  clauseId: string | null;
  findingId: string | null;
  evidenceId: string | null;
}

const EMPTY_CONTEXT: AuditWorkspaceContext = {
  scheduledAuditId: null,
  sessionId: null,
  departmentId: null,
  clauseId: null,
  findingId: null,
  evidenceId: null,
};

/**
 * Workspace-scoped state (Phases 1 and 3).
 *
 * The workspace shell is never destroyed while moving between tabs, so anything
 * held here survives tab switches. Phase 1 kept only the active tab. Phase 3
 * adds the workflow selection context (Step 15) and persists it (Step 11) so a
 * reload — not just a tab switch — resumes where the user left off.
 *
 * Persistence uses sessionStorage rather than localStorage: audit context is
 * per-working-session, and two browser tabs should be able to sit on different
 * audits without fighting over one key. The last-used *tab* stays in
 * localStorage, matching the Phase 1 behaviour it inherits.
 *
 * State only. No API calls, no routing — callers navigate; this records where
 * they went. That keeps the store safe to inject anywhere without pulling in
 * the whole audit dependency graph.
 */
@Injectable({ providedIn: 'root' })
export class AuditWorkspaceStateService {
  /** Capability the user is currently in; drives the tab strip's active state. */
  readonly activeTab = signal<AuditWorkspaceTabId>('overview');

  private readonly context = signal<AuditWorkspaceContext>(this.restoreContext());

  /** Current workflow selection. Read-only view — mutate via the setters below. */
  readonly workspaceContext = this.context.asReadonly();

  readonly scheduledAuditId = computed(() => this.context().scheduledAuditId);
  readonly sessionId = computed(() => this.context().sessionId);
  readonly departmentId = computed(() => this.context().departmentId);
  readonly clauseId = computed(() => this.context().clauseId);
  readonly findingId = computed(() => this.context().findingId);
  readonly evidenceId = computed(() => this.context().evidenceId);

  /** True once planning has handed an audit over to execution. */
  readonly hasActiveAudit = computed(() => this.context().sessionId !== null);

  /**
   * Workflow questions go to the engine.
   *
   * This service records *where the user is*; which stage that implies, what follows it and how far
   * along it sits are lifecycle decisions, and the engine is the only thing that makes those.
   */
  private readonly workflow = inject(AuditWorkflowEngine);

  /** Lifecycle stage implied by the active tab, or null outside the lifecycle. */
  readonly currentStage = computed<AuditWorkflowStage | null>(() =>
    this.workflow.stageForTab(this.activeTab()),
  );

  /** Per-stage completion for the workflow progress indicator. */
  readonly stageProgress = computed<readonly AuditWorkflowStageProgress[]>(() =>
    this.workflow.progressTo(this.currentStage()?.id ?? null),
  );

  /** The stage the workspace suggests moving to next, or null at the end. */
  readonly suggestedNextStage = computed<AuditWorkflowStage | null>(() =>
    this.workflow.nextStage(this.currentStage()?.id),
  );

  /** Opt-in key/value store for hosted pages that want to survive a tab switch. */
  private readonly scratch = new Map<string, unknown>();

  setActiveTab(tab: AuditWorkspaceTabId): void {
    if (this.activeTab() === tab) return;
    this.activeTab.set(tab);
    this.persistLastTab(tab);
  }

  /** Last capability used in this browser, for "resume where you left off". */
  readLastTab(): AuditWorkspaceTabId | null {
    try {
      const raw = localStorage.getItem(LAST_TAB_KEY);
      return (raw as AuditWorkspaceTabId | null) ?? null;
    } catch {
      return null;
    }
  }

  /**
   * Bind the workspace to an audit being executed.
   *
   * Clears the downstream selections (clause, finding, evidence) because they
   * belong to whichever audit was open before — carrying them into a different
   * audit would show the user records from another session.
   */
  setActiveAudit(sessionId: string, scheduledAuditId?: string | null): void {
    this.patch({
      sessionId,
      scheduledAuditId: scheduledAuditId ?? null,
      clauseId: null,
      findingId: null,
      evidenceId: null,
    });
  }

  setDepartment(departmentId: string | null): void {
    this.patch({ departmentId });
  }

  setClause(clauseId: string | null): void {
    this.patch({ clauseId });
  }

  setFinding(findingId: string | null): void {
    this.patch({ findingId });
  }

  setEvidence(evidenceId: string | null): void {
    this.patch({ evidenceId });
  }

  /** Drop the whole selection context — "start fresh", explicit user action only. */
  resetContext(): void {
    this.context.set({ ...EMPTY_CONTEXT });
    this.persistContext();
  }

  remember<T>(key: string, value: T): void {
    this.scratch.set(key, value);
  }

  recall<T>(key: string): T | undefined {
    return this.scratch.get(key) as T | undefined;
  }

  clear(key: string): void {
    this.scratch.delete(key);
  }

  private patch(changes: Partial<AuditWorkspaceContext>): void {
    this.context.update((current) => ({ ...current, ...changes }));
    this.persistContext();
  }

  private persistLastTab(tab: AuditWorkspaceTabId): void {
    try {
      localStorage.setItem(LAST_TAB_KEY, tab);
    } catch {
      /* storage unavailable (private mode) — non-fatal, tab state is still in memory */
    }
  }

  private persistContext(): void {
    try {
      sessionStorage.setItem(CONTEXT_KEY, JSON.stringify(this.context()));
    } catch {
      /* storage unavailable or quota exceeded — context stays in memory for this tab */
    }
  }

  private restoreContext(): AuditWorkspaceContext {
    try {
      const raw = sessionStorage.getItem(CONTEXT_KEY);
      if (!raw) return { ...EMPTY_CONTEXT };
      const parsed = JSON.parse(raw) as Partial<AuditWorkspaceContext>;
      // Spread over the empty shape so a persisted payload written by an older
      // build can never leave a newly added key undefined.
      return { ...EMPTY_CONTEXT, ...parsed };
    } catch {
      return { ...EMPTY_CONTEXT };
    }
  }
}
