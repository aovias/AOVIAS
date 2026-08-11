import type { AuditWorkspaceTabId } from './audit-workspace-tab.model';
import { SEED_AUDIT_WORKFLOW, type WorkflowStageDefinition } from './workflow-definition.model';

/**
 * Audit workflow — the lifecycle as the workspace navigates it.
 *
 * <p>
 * This file used to *be* the lifecycle: a literal array of stages plus positional helpers. The
 * stages, and the rules that were spread across `audit-lifecycle-state.model.ts`, now live together
 * in {@link SEED_AUDIT_WORKFLOW} as a declarative definition that configuration can replace.
 * </p>
 *
 * <p>
 * What remains here is the *positional* view over the platform-default lifecycle: order, next,
 * previous, and tab mapping. These are pure functions over the seed and are what the existing
 * callers and specs use. Anything that must honour a tenant's configured lifecycle goes through
 * {@link AuditWorkflowEngine} instead — which is why the engine, not this file, is what the
 * workspace services consume.
 * </p>
 *
 * <p>
 * Business rules are not encoded here. Whether a stage may be entered is a server concern; this
 * view only expresses the intended order of work.
 * </p>
 */

/**
 * The id of a lifecycle stage.
 *
 * <p>
 * Deliberately `string` rather than the six-member union it replaces. That union claimed a closed
 * set, and the claim stopped being true the moment the lifecycle became configurable: a stage a
 * tenant adds is valid at runtime and would not typecheck against it. The union was not describing
 * the domain, it was contradicting it.
 * </p>
 *
 * <p>
 * The trade is real — `stage: 'findigns'` in a page no longer fails to compile. The engine resolves
 * an unknown id to `null` and the surfaces that consume it render nothing, so a typo is a missing
 * status bar rather than a crash. The platform-default ids remain
 * `planning · execution · evidence · findings · capa · reports`.
 * </p>
 */
export type AuditWorkflowStageId = string;

/**
 * A stage as the workspace reads it.
 *
 * Structurally the definition, rules included — a consumer that only wants `label` and `route`
 * simply ignores the rest, and giving the navigation surfaces a narrowed copy would mean two shapes
 * for one concept.
 */
export type AuditWorkflowStage = WorkflowStageDefinition;

/**
 * The platform-default lifecycle, in order.
 *
 * Derived from the seed definition rather than declared, so the ordering cannot disagree with the
 * definition the rules are attached to.
 */
export const AUDIT_WORKFLOW_STAGES: readonly AuditWorkflowStage[] = [
  ...SEED_AUDIT_WORKFLOW.stages,
].sort((a, b) => a.order - b.order);

/** Completion state of a stage, for the workflow progress indicator. */
export type AuditWorkflowStageStatus = 'complete' | 'current' | 'pending';

export interface AuditWorkflowStageProgress {
  readonly stage: AuditWorkflowStage;
  readonly status: AuditWorkflowStageStatus;
}

export function getStage(id: AuditWorkflowStageId): AuditWorkflowStage {
  // Every AuditWorkflowStageId has an entry in the seed, so this cannot miss.
  return AUDIT_WORKFLOW_STAGES.find((s) => s.id === id)!;
}

export function stageIndex(id: AuditWorkflowStageId): number {
  return AUDIT_WORKFLOW_STAGES.findIndex((s) => s.id === id);
}

/** The stage that follows `id`, or null at the end of the lifecycle. */
export function nextStage(id: AuditWorkflowStageId): AuditWorkflowStage | null {
  const idx = stageIndex(id);
  if (idx < 0) return null;
  return AUDIT_WORKFLOW_STAGES[idx + 1] ?? null;
}

/** The stage before `id`, or null at the entry stage. */
export function previousStage(id: AuditWorkflowStageId): AuditWorkflowStage | null {
  const idx = stageIndex(id);
  if (idx <= 0) return null;
  return AUDIT_WORKFLOW_STAGES[idx - 1] ?? null;
}

/**
 * Which stage a capability tab belongs to.
 *
 * Tabs that sit outside the lifecycle (overview, intelligence, administration) return null — they
 * are vantage points over the workflow, not steps in it, so the progress bar reports no current
 * stage while the user is in them.
 */
export function stageForTab(tab: AuditWorkspaceTabId): AuditWorkflowStage | null {
  return AUDIT_WORKFLOW_STAGES.find((s) => s.tab === tab) ?? null;
}

/**
 * Progress across the whole lifecycle relative to the stage the user is in.
 *
 * Positional only: everything before the current stage reads as complete. It describes where the
 * user is in the sequence, not whether the underlying work actually finished — that is what
 * `deriveLifecycleState` is for.
 */
export function buildStageProgress(
  current: AuditWorkflowStageId | null,
): readonly AuditWorkflowStageProgress[] {
  const currentIdx = current ? stageIndex(current) : -1;
  return AUDIT_WORKFLOW_STAGES.map((stage, idx) => ({
    stage,
    status:
      currentIdx < 0 || idx > currentIdx
        ? ('pending' as const)
        : idx === currentIdx
          ? ('current' as const)
          : ('complete' as const),
  }));
}
