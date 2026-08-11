/** Shared deep-link builder for the authoritative Review Queue surface (Sprint 5A). */

export const EVIDENCE_REVIEW_QUEUE_PATH = '/app/evidence/review-queue';

export interface EvidenceReviewQueueNavParams {
  evidenceId?: string | null;
  objectiveElementId?: string | null;
  objectiveElementCode?: string | null;
  /** Pre-filter queue status; omit for default awaiting-review. */
  status?: string | null;
}

export function evidenceReviewQueueQuery(
  params: EvidenceReviewQueueNavParams,
): Record<string, string> {
  const q: Record<string, string> = {};
  const eid = (params.evidenceId ?? '').trim();
  const oeId = (params.objectiveElementId ?? '').trim();
  const code = (params.objectiveElementCode ?? '').trim();
  const status = (params.status ?? '').trim();
  if (eid) q['evidenceId'] = eid;
  if (oeId) q['objectiveElementId'] = oeId;
  if (code) q['search'] = code;
  if (status) q['status'] = status;
  return q;
}
