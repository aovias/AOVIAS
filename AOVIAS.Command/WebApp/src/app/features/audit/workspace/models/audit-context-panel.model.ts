/**
 * Contextual side panel model — Phase 3, Steps 8 and 10.
 *
 * The panel is persistent chrome whose *content* tracks what the user is
 * currently looking at. That requires naming the thing being looked at, which
 * is what {@link AuditContextSubject} is for: pages declare their subject, and
 * the panel shows sections relevant to it.
 *
 * Data-only, like the workflow model — no Angular here, so the panel, the
 * contributing pages and any future AI context builder share one vocabulary.
 */

/**
 * What the user is currently working on.
 *
 * Drives which sections appear. `none` is the honest state for a surface that
 * has no single subject (an empty list, a dashboard) — better than pretending
 * a subject exists and rendering sections about nothing.
 */
export type AuditContextSubjectKind =
  | 'none'
  | 'planning'
  | 'audit'
  | 'clause'
  | 'evidence'
  | 'finding'
  | 'capa';

export interface AuditContextSubject {
  readonly kind: AuditContextSubjectKind;
  /** Record the subject refers to, when it is a specific one. */
  readonly id?: string | null;
  /** Human label for the panel header, e.g. "OBS-08-1" or "ICU Internal Audit". */
  readonly title?: string | null;
}

export const NO_CONTEXT_SUBJECT: AuditContextSubject = { kind: 'none' };

/**
 * A single line in a panel section.
 *
 * `route` turns the item into a link — this is how Step 10's "linked documents
 * reachable from everywhere" works without a separate documents page. Items
 * without a route are read-outs (a score, a status, a count).
 */
export interface AuditContextItem {
  readonly label: string;
  readonly value?: string | null;
  /** Router link for navigable items; omit for plain read-outs. */
  readonly route?: string | readonly (string | number)[];
  /** Secondary text, e.g. "raised 3 audits ago". */
  readonly hint?: string | null;
}

export interface AuditContextSection {
  readonly id: string;
  readonly title: string;
  readonly items: readonly AuditContextItem[];
  /** Shown instead of the items when the section resolved to nothing. */
  readonly emptyText?: string;
}

/**
 * Page-context string handed to the Copilot API for the current subject.
 *
 * The AI panel must reason about what the user is looking at rather than the
 * workspace in general, and the backend keys its persona rules off this string.
 * Centralised here so the panel and any other caller cannot drift apart.
 */
export function copilotPageContextFor(subject: AuditContextSubject): string {
  switch (subject.kind) {
    case 'planning':
      return 'AuditPlanning';
    case 'audit':
      return 'AuditExecution';
    case 'clause':
      return 'AuditAssessment';
    case 'evidence':
      return 'AuditEvidence';
    case 'finding':
      return 'AuditFindings';
    case 'capa':
      return 'AuditCapa';
    case 'none':
    default:
      return 'General';
  }
}
