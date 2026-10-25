import type { TaskListItem } from './models';

/** True when merged evidence includes items pending review (or missing OE link). Server enforces the same rule on complete. */
export function taskHasPendingEvidenceReviews(t: Pick<TaskListItem, 'evidencePendingReviewCount'>): boolean {
  return (t.evidencePendingReviewCount ?? 0) > 0;
}
