/**
 * Authoritative evidence verification presentation from EvidenceVerificationStateMachine API keys.
 * Shared by Evidence, Journey, Tasks, and Reporting surfaces (Sprint 5A).
 */

export type EvidencePresentationStatus =
  | 'draft'
  | 'uploaded'
  | 'submitted'
  | 'pendingReview'
  | 'approved'
  | 'rejected'
  | 'needsMoreEvidence'
  | 'verified';

/** Obligation-level verification uses Verified; evidence items use Approved for the same proof state. */
export type EvidenceStatusPresentationContext = 'evidence' | 'obligation';

export const EVIDENCE_STATUS_LABEL: Record<EvidencePresentationStatus, string> = {
  draft: 'Draft',
  uploaded: 'Uploaded',
  submitted: 'Submitted',
  pendingReview: 'Pending Review',
  approved: 'Approved',
  rejected: 'Rejected',
  needsMoreEvidence: 'Needs More Evidence',
  verified: 'Verified',
};

const ALIAS_TO_CANONICAL: Record<string, EvidencePresentationStatus> = {
  draft: 'draft',
  uploaded: 'uploaded',
  submitted: 'submitted',
  pendingreview: 'pendingReview',
  pending: 'pendingReview',
  awaitingreview: 'pendingReview',
  inreview: 'pendingReview',
  approved: 'approved',
  verified: 'approved',
  accepted: 'approved',
  reviewed: 'approved',
  complete: 'approved',
  completed: 'approved',
  rejected: 'rejected',
  declined: 'rejected',
  needsmoreevidence: 'needsMoreEvidence',
  needsmore: 'needsMoreEvidence',
  needsmoreinfo: 'needsMoreEvidence',
  returnforrevision: 'needsMoreEvidence',
  revisionrequested: 'needsMoreEvidence',
};

const OBLIGATION_ALIAS: Record<string, EvidencePresentationStatus> = {
  ...ALIAS_TO_CANONICAL,
  verified: 'verified',
  approved: 'verified',
  notverified: 'pendingReview',
  missing: 'draft',
};

/** Normalize any raw API / legacy string to a canonical presentation status key. */
export function normalizeEvidenceStatus(
  raw: string | null | undefined,
  context: EvidenceStatusPresentationContext = 'evidence',
): EvidencePresentationStatus {
  const compact = (raw ?? '').trim().toLowerCase().replace(/[\s_-]+/g, '');
  if (!compact) {
    return 'pendingReview';
  }
  const map = context === 'obligation' ? OBLIGATION_ALIAS : ALIAS_TO_CANONICAL;
  return map[compact] ?? 'pendingReview';
}

/** User-facing label — sole source for evidence verification status text in UI. */
export function evidenceStatusLabel(
  raw: string | null | undefined,
  context: EvidenceStatusPresentationContext = 'evidence',
): string {
  const key = normalizeEvidenceStatus(raw, context);
  if (context === 'obligation' && key === 'approved') {
    return EVIDENCE_STATUS_LABEL.verified;
  }
  return EVIDENCE_STATUS_LABEL[key] ?? EVIDENCE_STATUS_LABEL.pendingReview;
}

/** CSS / data-attribute token for badge styling (matches canonical key). */
export function evidenceStatusToken(
  raw: string | null | undefined,
  context: EvidenceStatusPresentationContext = 'evidence',
): EvidencePresentationStatus {
  return normalizeEvidenceStatus(raw, context);
}

export function isEvidenceAwaitingReview(raw: string | null | undefined): boolean {
  const key = normalizeEvidenceStatus(raw, 'evidence');
  return key === 'pendingReview' || key === 'submitted' || key === 'uploaded';
}

export function canReviewEvidenceInQueue(raw: string | null | undefined): boolean {
  const key = normalizeEvidenceStatus(raw, 'evidence');
  return key === 'pendingReview' || key === 'submitted';
}
