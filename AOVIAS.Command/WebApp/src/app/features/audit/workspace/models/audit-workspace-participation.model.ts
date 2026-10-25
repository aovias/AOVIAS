import type { AuditWorkflowStageId } from './audit-workflow.model';
import type { AuditLifecycleFacts } from './audit-lifecycle-state.model';
import type { AuditContextSubject } from './audit-context-panel.model';
import type { AuditWorkspaceAction } from '../services/audit-workspace-actions.service';

/**
 * Workspace participation contract — Stage 1.1.
 *
 * Phase 3 built the workspace registries but left each page to wire itself into
 * them, so participation drifted: one page registered actions and a context
 * subject, the rest registered nothing. This model is the single contract every
 * Audit page implements instead.
 *
 * Everything here is declarative data. A page describes what it is showing and
 * what can be done to it; the workspace decides how to present that. Pages never
 * talk to the individual registries, which is what stops per-page approaches
 * from reappearing.
 *
 * Ids and labels only — never loaded entities. Pages own their data fetching;
 * participation is the shared summary of it.
 */

/** A document reachable from the current record (Step 6). */
export interface AuditLinkedDocument {
  readonly id: string;
  readonly title: string;
  /** Policy, SOP, checklist, evidence file, report — shown as the item's hint. */
  readonly kind: string;
  readonly route?: string | readonly (string | number)[];
}

/** Another record that relates to the current subject (Step 5). */
export interface AuditRelatedRecord {
  readonly id: string;
  readonly label: string;
  /** Finding, CAPA, risk, session, evidence — groups the panel listing. */
  readonly kind: string;
  readonly route?: string | readonly (string | number)[];
  /** Short state read-out, e.g. "Open", "Overdue", "Verified". */
  readonly status?: string;
}

/** One entry in the shared workspace history (Step 8). */
export interface AuditTimelineEvent {
  readonly id: string;
  readonly label: string;
  /** ISO timestamp. Rendered relative, sorted descending. */
  readonly at: string;
  readonly actor?: string;
}

/**
 * Where the current *record* sits in its lifecycle (Step 7).
 *
 * Distinct from the positional progress the status bar derives from the active
 * tab: that says which capability the user opened, this says what state the
 * record is actually in. Pages report it because only they have loaded the
 * record; nothing else in the workspace can know it.
 */
export interface AuditRecordWorkflowState {
  readonly stage: AuditWorkflowStageId;
  /** Record status as the domain words it — "Scheduled", "Under review", "Open". */
  readonly statusLabel: string;
  /** What the lifecycle permits here; drives which toolbar actions are live. */
  readonly allowedActions?: readonly string[];
  /** What it forbids, with the reason shown on the disabled control. */
  readonly blockedActions?: readonly { readonly actionId: string; readonly reason: string }[];
  /** What has to be true to advance, e.g. "All observations triaged". */
  readonly completionCriteria?: string;
}

/**
 * Structured context handed to the AI panel (Step 4).
 *
 * The page-context string alone produced generic guidance because it only says
 * which screen is open. `facts` carries the specifics — framework, department,
 * the record's own identifiers — so guidance can reference what the user is
 * actually looking at.
 */
export interface AuditAiContext {
  /** Backend persona key; derived from the subject when omitted. */
  readonly pageContext?: string;
  readonly framework?: string | null;
  readonly department?: string | null;
  /** Free-form domain facts. Keys are stable, values display-ready. */
  readonly facts?: Readonly<Record<string, string>>;
}

/**
 * The full declaration a page makes to the workspace.
 *
 * Every field beyond `stage` and `subject` is optional: a page contributes what
 * it genuinely has. Declaring an empty array and omitting a field mean different
 * things — an empty array is "none exist", omission is "not applicable here" —
 * and the panel renders them differently.
 */
export interface AuditWorkspaceParticipation {
  /**
   * Lifecycle stage this page belongs to; null for surfaces outside the
   * lifecycle (overview, intelligence, administration).
   */
  readonly stage: AuditWorkflowStageId | null;
  readonly subject: AuditContextSubject;
  readonly actions?: readonly AuditWorkspaceAction[];
  readonly relatedRecords?: readonly AuditRelatedRecord[];
  readonly documents?: readonly AuditLinkedDocument[];
  readonly timeline?: readonly AuditTimelineEvent[];
  readonly workflowState?: AuditRecordWorkflowState;
  readonly aiContext?: AuditAiContext;
  /**
   * Counts the workspace uses to derive real lifecycle progress (Stage 1.4).
   *
   * Report only what the page actually loaded — an omitted count means "not
   * known here", which the derivation treats differently from zero.
   */
  readonly lifecycleFacts?: AuditLifecycleFacts;
}

/** Empty participation — the state the workspace holds when no page has declared. */
export const NO_PARTICIPATION: AuditWorkspaceParticipation = {
  stage: null,
  subject: { kind: 'none' },
};
