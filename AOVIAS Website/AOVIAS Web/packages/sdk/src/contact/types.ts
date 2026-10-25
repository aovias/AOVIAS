/**
 * Contact / inquiry domain models.
 */

export type ContactIntent =
  | 'contact'
  | 'demo'
  | 'enterprise'
  | 'partner'
  | 'newsletter';

export type ContactSubmission = {
  intent: ContactIntent;
  payload: Readonly<Record<string, unknown>>;
  csrfToken?: string;
  submittedAt?: string;
};

export type ContactSubmissionResult = {
  ok: boolean;
  submissionId?: string;
  message?: string;
  followUp?: string;
  retryable?: boolean;
};
