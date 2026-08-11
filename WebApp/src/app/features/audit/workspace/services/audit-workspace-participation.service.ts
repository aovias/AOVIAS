import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import {
  AiPageContextService,
  type AiPageContext,
} from '../../../../core/services/ai-page-context.service';
import {
  copilotPageContextFor,
  type AuditContextSubjectKind,
} from '../models/audit-context-panel.model';
import { AuditWorkflowEngine } from './audit-workflow.engine';
import {
  NO_PARTICIPATION,
  type AuditWorkspaceParticipation,
} from '../models/audit-workspace-participation.model';
import { AuditContextPanelService } from './audit-context-panel.service';
import { AuditWorkspaceActionsService } from './audit-workspace-actions.service';

/**
 * Workspace participation registry — Stage 1.1.
 *
 * One entry point for every Audit page. A page declares what it is showing; this
 * service fans that declaration out to the context panel, the action toolbar and
 * the AI panel, and holds the parts (related records, documents, timeline,
 * workflow state) the panel reads directly.
 *
 * Why a facade rather than pages calling the registries themselves: Stage 1.1
 * exists precisely because per-page wiring drifted. With one method to call there
 * is one thing to get right, one place to change when the contract grows, and no
 * way for a page to half-participate by registering actions but forgetting to
 * clear its subject.
 *
 * Single-participant by design. Exactly one routed page owns the workspace at a
 * time, so a later declaration replaces the earlier one wholesale rather than
 * merging — merging would let a departed page's documents linger under the next
 * page's subject.
 */
/**
 * Audit subject → the app-wide AI assistant's context vocabulary.
 *
 * The assistant's union predates the workspace and is shared across modules, so
 * audit subjects fold into the nearest existing member rather than the union
 * being widened for Audit's benefit. Subjects with no counterpart fall back to
 * `General`, which is what the assistant showed before Stage 1.1 anyway.
 */
const ASSISTANT_CONTEXT: Record<AuditContextSubjectKind, AiPageContext> = {
  none: 'General',
  planning: 'General',
  audit: 'General',
  clause: 'Evidence',
  evidence: 'Evidence',
  finding: 'Findings',
  capa: 'Dcc',
};

@Injectable({ providedIn: 'root' })
export class AuditWorkspaceParticipationService {
  private readonly actions = inject(AuditWorkspaceActionsService);
  private readonly panel = inject(AuditContextPanelService);
  private readonly assistant = inject(AiPageContextService);
  private readonly workflow = inject(AuditWorkflowEngine);

  private readonly state = signal<AuditWorkspaceParticipation>(NO_PARTICIPATION);

  /** Token of the page that currently owns the workspace. */
  private owner: symbol | null = null;

  readonly participation = this.state.asReadonly();

  readonly relatedRecords = computed(() => this.state().relatedRecords ?? []);
  readonly documents = computed(() => this.state().documents ?? []);
  readonly workflowState = computed(() => this.state().workflowState ?? null);

  /**
   * Lifecycle progress derived from what the active page reported.
   *
   * The workspace's single source of "how far along is this audit" — the status
   * bar, the recommended next action and the AI context all read it, so they
   * cannot disagree about where the work stands.
   *
   * The derivation itself belongs to the engine, which holds the lifecycle this
   * organization is configured with; this service only supplies the facts.
   */
  readonly lifecycle = computed(() => this.workflow.evaluate(this.state().lifecycleFacts ?? {}));

  /** True once a page has reported anything to derive progress from. */
  readonly hasLifecycleFacts = computed(() => this.state().lifecycleFacts !== undefined);

  /** Newest first — a timeline reads backwards from what just happened. */
  readonly timeline = computed(() =>
    [...(this.state().timeline ?? [])].sort((a, b) => b.at.localeCompare(a.at)),
  );

  /**
   * Structured AI context for the guidance panel.
   *
   * Falls back to deriving the page context from the subject so a page that
   * declares no `aiContext` still gets guidance scoped to the right screen
   * rather than the generic workspace.
   */
  readonly aiContext = computed(() => {
    const current = this.state();
    const ai = current.aiContext;
    return {
      pageContext: ai?.pageContext ?? copilotPageContextFor(current.subject),
      framework: ai?.framework ?? null,
      department: ai?.department ?? null,
      facts: ai?.facts ?? {},
      subjectKind: current.subject.kind,
      subjectId: current.subject.id ?? null,
      stage: current.workflowState?.stage ?? current.stage ?? null,
      statusLabel: current.workflowState?.statusLabel ?? null,
    };
  });

  /**
   * Declare (or re-declare) the calling page's participation.
   *
   * Safe to call repeatedly as the page's data loads — each call replaces the
   * previous declaration for that owner, which is how a page publishes counts
   * and related records once its requests resolve.
   */
  declare(owner: symbol, participation: AuditWorkspaceParticipation): void {
    this.owner = owner;
    this.state.set(participation);

    this.panel.setSubject(participation.subject);
    this.panel.setSections([]);

    if (participation.stage) {
      this.actions.register(participation.stage, participation.actions ?? []);
    }

    // Keep the app-wide assistant in step with the workspace. Declaring once
    // feeds both, which is what removes the separate per-page
    // `setPageContext` calls this contract replaces.
    this.assistant.setPageContext({
      pageContext: ASSISTANT_CONTEXT[participation.subject.kind],
      entityType: participation.subject.kind === 'none' ? null : participation.subject.kind,
      entityId: participation.subject.id ?? null,
      label: participation.subject.title ?? null,
    });
  }

  /**
   * Withdraw a page's participation.
   *
   * Ignored when `owner` no longer holds the workspace. Angular constructs the
   * next routed component before destroying the previous one, so a late teardown
   * would otherwise wipe the incoming page's freshly declared context.
   */
  withdraw(owner: symbol): void {
    if (this.owner !== owner) return;

    const stage = this.state().stage;
    if (stage) this.actions.clear(stage);

    this.owner = null;
    this.state.set(NO_PARTICIPATION);
    this.panel.clear();
    this.assistant.clearPageContext();
  }
}

/**
 * Bind the calling component to the workspace for its lifetime.
 *
 * Returns a handle whose `set()` declares participation; teardown is registered
 * against the component's own {@link DestroyRef}, so pages need no `ngOnDestroy`
 * and cannot leak a registration by forgetting one.
 *
 * Call from a field initializer or constructor — `inject()` requires an
 * injection context.
 */
export function injectAuditWorkspace(): {
  set(participation: AuditWorkspaceParticipation): void;
} {
  const registry = inject(AuditWorkspaceParticipationService);
  const destroyRef = inject(DestroyRef);
  const owner = Symbol('audit-workspace-participant');

  destroyRef.onDestroy(() => registry.withdraw(owner));

  return {
    set: (participation) => registry.declare(owner, participation),
  };
}
