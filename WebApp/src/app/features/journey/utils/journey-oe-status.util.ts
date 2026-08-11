import { ObjectiveElement } from '../models/objective-element.model';

/**
 * Per-OE journey workflow state for the execution workspace (Standards → OE → Journey).
 * Server populates `ObjectiveElement.journeyWorkflowStatus` from `ObjectiveJourneys` (OE wizard).
 */
export type JourneyWorkflowStatus = 'not_started' | 'in_progress' | 'completed';

export function journeyWorkflowStatusLabel(status: JourneyWorkflowStatus): string {
  switch (status) {
    case 'not_started':
      return 'Not Started';
    case 'in_progress':
      return 'In Progress';
    case 'completed':
      return 'Completed';
  }
}

function parseJourneyWorkflowStatus(raw: string | null | undefined): JourneyWorkflowStatus | null {
  const t = (raw ?? '').trim();
  if (t === '1' || t === '0') {
    if (t === '1') {
      return 'in_progress';
    }
    return 'not_started';
  }
  if (t === '2') {
    return 'completed';
  }
  const s = t.toLowerCase().replace(/-/g, '_');
  if (s === 'completed' || s === 'complete') {
    return 'completed';
  }
  if (s === 'in_progress' || s === 'inprogress') {
    return 'in_progress';
  }
  if (s === 'not_started' || s === 'notstarted' || s === 'draft' || s === 'none' || s === '') {
    return s === '' ? null : 'not_started';
  }
  return null;
}

/**
 * Per-OE journey column: driven by server `journeyWorkflowStatus` (ObjectiveJourneys / OE wizard).
 * “Completed” is never inferred from evidence or score; missing API values fall back to activity hints
 * (never to completed) so list rows match wizard completion.
 */
export function oeJourneyStatus(oe: ObjectiveElement): JourneyWorkflowStatus {
  const fromApi = parseJourneyWorkflowStatus(oe.journeyWorkflowStatus);
  if (fromApi) {
    return fromApi;
  }
  if ((oe.journeyWorkflowStatus ?? '').trim() !== '') {
    return 'not_started';
  }
  if (oe.isNa) {
    return 'completed';
  }
  const evCount = oe.evidence?.length ?? 0;
  const es = oe.evidenceStatus;
  if (es === 'pending_verification' || evCount > 0) {
    return 'in_progress';
  }
  return 'not_started';
}

/** Evidence column — consistent when `evidence[]` is not loaded on list rows. */
export function oeEvidenceCountLabel(oe: ObjectiveElement): string {
  const n = oe.evidence?.length ?? 0;
  if (n > 0) {
    return n === 1 ? '1 file' : `${n} files`;
  }
  if (oe.evidenceStatus === 'complete' || oe.evidenceVerifiedFlag === true) {
    return '1 file';
  }
  if (oe.evidenceStatus === 'pending_verification') {
    return 'Pending';
  }
  return '0 files';
}
