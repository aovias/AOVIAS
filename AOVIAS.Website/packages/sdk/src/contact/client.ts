import type { WithSdkInfo } from '../core/client';
import type { ContactSubmission, ContactSubmissionResult } from './types';

/**
 * Contact SDK — form submissions without email/backend delivery in R1.5.
 */
export type ContactClient = WithSdkInfo & {
  submitContact: (
    input: ContactSubmission,
  ) => Promise<ContactSubmissionResult>;

  submitDemoRequest: (
    input: ContactSubmission,
  ) => Promise<ContactSubmissionResult>;

  submitEnterpriseInquiry: (
    input: ContactSubmission,
  ) => Promise<ContactSubmissionResult>;

  submitPartnerInquiry: (
    input: ContactSubmission,
  ) => Promise<ContactSubmissionResult>;

  submitNewsletter: (
    input: ContactSubmission,
  ) => Promise<ContactSubmissionResult>;
};
