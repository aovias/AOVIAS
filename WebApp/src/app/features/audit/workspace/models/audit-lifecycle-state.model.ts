import {
  evaluateCondition,
  formatWorkflowMessage,
  type WorkflowFacts,
} from './workflow-condition.model';
import {
  SEED_AUDIT_WORKFLOW,
  type WorkflowDefinition,
  type WorkflowMessageRule,
  type WorkflowStageDefinition,
} from './workflow-definition.model';
import type {
  AuditWorkflowStage,
  AuditWorkflowStageId,
  AuditWorkflowStageStatus,
} from './audit-workflow.model';

/**
 * Lifecycle state derivation.
 *
 * <p>
 * The rules this file used to hold — completion, outstanding work, entry prerequisites and
 * recommendations — were four `switch` statements over the stage id, one arm per stage. They now
 * live in {@link WorkflowDefinition} as conditions over facts, and what remains here is the
 * evaluation that turns a definition plus reported facts into lifecycle state.
 * </p>
 *
 * <p>
 * **Behaviour is unchanged.** {@link SEED_AUDIT_WORKFLOW} is a transcription of those switches, and
 * this file's spec — written against the old implementation and untouched — is what proves it.
 * </p>
 *
 * <p>
 * Still pure and still data-only, so the rules stay testable and every surface — status bar, drawer,
 * AI context — reads one derivation. The engine supplies a configured definition; callers that just
 * want platform behaviour need pass nothing.
 * </p>
 */

/**
 * What the workspace knows about the current audit.
 *
 * Every field is optional because a page reports only what it has loaded. An `undefined` count means
 * "not known here", which is deliberately different from `0` meaning "none exist" — the first must
 * not mark a stage incomplete on a page that simply never fetched it. The condition language keeps
 * that distinction: `known`, `positive` and `zero` are three different tests.
 */
export interface AuditLifecycleFacts {
  readonly hasScheduledAudit?: boolean;
  readonly hasSession?: boolean;
  readonly sessionClosed?: boolean;
  readonly evidenceCount?: number;
  readonly observationCount?: number;
  readonly findingCount?: number;
  readonly openFindingCount?: number;
  readonly capaCount?: number;
  readonly openCapaCount?: number;
  readonly verifiedCapaCount?: number;
  readonly reportAvailable?: boolean;
}

export interface AuditStageState {
  readonly stage: AuditWorkflowStage;
  readonly status: AuditWorkflowStageStatus;
  /** What still has to happen here, phrased for the user. Empty when settled. */
  readonly outstanding: readonly string[];
  /** False when an upstream prerequisite has not been met. */
  readonly reachable: boolean;
  /** Why it is not reachable — guidance, never a blocking error. */
  readonly blockedReason?: string;
  /**
   * What the workspace suggests doing at this stage given current facts.
   *
   * Distinct from `outstanding`, which states what is *missing*. These say what is worth *doing*
   * given the numbers, so the guidance changes as the audit progresses instead of repeating the
   * same advice regardless of state.
   */
  readonly recommendations: readonly string[];
}

export interface AuditLifecycleState {
  readonly stages: readonly AuditStageState[];
  /** Furthest stage with real progress; drives the "you are here" marker. */
  readonly currentStageId: AuditWorkflowStageId | null;
  /** The single action the workspace recommends next. */
  readonly recommendedAction: string | null;
  readonly recommendedStageId: AuditWorkflowStageId | null;
  /** Completed stages over total, for a coarse progress read-out. */
  readonly completedCount: number;
}

/** Messages from every rule whose condition holds, in declaration order. */
function messages(rules: readonly WorkflowMessageRule[], facts: WorkflowFacts): string[] {
  return rules
    .filter((rule) => evaluateCondition(rule.when, facts))
    .map((rule) => formatWorkflowMessage(rule.message, facts));
}

/** Why a stage cannot be entered, or `undefined` when nothing blocks it. */
function blockedReason(stage: WorkflowStageDefinition, facts: WorkflowFacts): string | undefined {
  const rule = stage.entryBlockedWhen;
  if (!rule || !evaluateCondition(rule.when, facts)) return undefined;
  return formatWorkflowMessage(rule.message, facts);
}

/**
 * Derive the whole lifecycle from reported facts.
 *
 * The current stage is the earliest incomplete one — where the work actually sits — rather than
 * whichever tab happens to be open. Stages after it read as pending even if some later work exists,
 * because a lifecycle that claims to be further along than its earliest gap would mislead.
 */
export function deriveLifecycleState(
  facts: AuditLifecycleFacts,
  definition: WorkflowDefinition = SEED_AUDIT_WORKFLOW,
): AuditLifecycleState {
  const reported = facts as WorkflowFacts;

  const evaluated = definition.stages.map((stage) => ({
    stage,
    satisfied: evaluateCondition(stage.completeWhen, reported),
    outstanding: messages(stage.outstanding, reported),
    recommendations: messages(stage.recommendations, reported),
    blockedReason: blockedReason(stage, reported),
  }));

  /**
   * The earliest unfinished stage is where the work sits. `-1` means the whole lifecycle is
   * satisfied — there is then no "current" stage, and marking the last one current would understate
   * a finished audit as still in progress.
   */
  const currentIdx = evaluated.findIndex((e) => !e.satisfied);

  const stages: AuditStageState[] = evaluated.map((e, idx) => {
    /**
     * Completion is **monotonic**: a stage reads complete only when it is satisfied *and* nothing
     * before it is outstanding (DEF-RC2W7-001).
     *
     * Each stage's `completeWhen` is evaluated independently, so a later stage can be satisfied
     * while an earlier one is not — a session opened without a scheduled audit satisfies Execution
     * while Planning is still open. Rendering that literally produced a progress bar reading
     * "Planning current · Execution ✓ complete", which is not a state any audit can be in. The rules
     * were each correct; composing them in declared order is what makes the sequence readable.
     */
    const complete = e.satisfied && (currentIdx < 0 || idx < currentIdx);

    return {
      stage: e.stage,
      status: complete
        ? ('complete' as const)
        : idx === currentIdx
          ? ('current' as const)
          : ('pending' as const),
      outstanding: e.outstanding,
      // A finished stage has nothing left to block it. Keeping the reason would tell an operator to
      // "schedule an audit before executing it" beside a stage already marked done.
      reachable: complete || !e.blockedReason,
      blockedReason: complete ? undefined : e.blockedReason,
      recommendations: e.recommendations,
    };
  });

  const current = currentIdx < 0 ? null : stages[currentIdx];
  const recommended = current?.outstanding[0] ?? null;

  return {
    stages,
    currentStageId: current?.stage.id ?? null,
    recommendedAction: recommended,
    recommendedStageId: recommended && current ? current.stage.id : null,
    completedCount: stages.filter((s) => s.status === 'complete').length,
  };
}
