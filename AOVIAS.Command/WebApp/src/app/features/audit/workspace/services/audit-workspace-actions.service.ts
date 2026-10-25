import { Injectable, signal } from '@angular/core';
import type { AuditWorkflowStageId } from '../models/audit-workflow.model';

/**
 * A single operational action offered by the workspace toolbar.
 *
 * Actions are verbs that change state — "Start audit", "Create CAPA", "Close".
 * Navigation never belongs here: moving between capabilities is the tab strip's
 * job and moving along the lifecycle is the status bar's, so a link in the
 * toolbar would be a third, competing way to travel (Step 14).
 */
export interface AuditWorkspaceAction {
  readonly id: string;
  readonly label: string;
  /** `primary` is the stage's main move; at most one per stage reads well. */
  readonly variant?: 'primary' | 'default' | 'danger';
  /** Renders the action inert with a tooltip explaining why. */
  readonly disabled?: boolean;
  readonly disabledReason?: string;
  readonly run: () => void;
}

/**
 * Contextual action registry — Phase 3, Step 14.
 *
 * The toolbar is contextual, so its contents cannot be a fixed list in the
 * shell: only the page currently hosting a stage knows which operations are
 * available and how to perform them.
 *
 * So pages *contribute* their actions and the shell renders whatever is
 * registered for the active stage. The important consequence is that the
 * toolbar can never show an action nothing is listening to — if it renders, a
 * handler exists. That is why there is no static default action list.
 *
 * Registration is per stage and last-writer-wins. A stage is hosted by one
 * routed page at a time, and pages clear on destroy, so there is no ambiguity
 * about which page owns a stage's actions.
 */
@Injectable({ providedIn: 'root' })
export class AuditWorkspaceActionsService {
  private readonly registry = signal<
    Partial<Record<AuditWorkflowStageId, readonly AuditWorkspaceAction[]>>
  >({});

  /** Actions currently contributed for `stage`; empty when nothing registered. */
  actionsFor(stage: AuditWorkflowStageId | null): readonly AuditWorkspaceAction[] {
    if (!stage) return [];
    return this.registry()[stage] ?? [];
  }

  /** Read-only registry handle so components can react to contributions. */
  readonly contributions = this.registry.asReadonly();

  /**
   * Publish the action set for a stage, replacing anything previously there.
   *
   * Call from the hosting page whenever its available operations change — the
   * toolbar is a projection of page state, not a snapshot taken at init.
   */
  register(stage: AuditWorkflowStageId, actions: readonly AuditWorkspaceAction[]): void {
    this.registry.update((current) => ({ ...current, [stage]: actions }));
  }

  /**
   * Withdraw a stage's actions. Pages must call this on destroy, otherwise a
   * toolbar button would outlive the page able to service it.
   */
  clear(stage: AuditWorkflowStageId): void {
    this.registry.update((current) => {
      if (!(stage in current)) return current;
      const next = { ...current };
      delete next[stage];
      return next;
    });
  }
}
