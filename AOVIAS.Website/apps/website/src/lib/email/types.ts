/**
 * Provider-agnostic email delivery for contact enquiries.
 * Credentials stay server-side only (never NEXT_PUBLIC_*).
 */

export type EmailAddress = {
  email: string;
  name?: string;
};

export type EmailMessage = {
  to: EmailAddress | EmailAddress[];
  from: EmailAddress;
  replyTo?: EmailAddress;
  subject: string;
  text: string;
  html?: string;
};

export type EmailSendResult = {
  ok: boolean;
  id?: string;
  error?: string;
};

export type EmailProvider = {
  readonly id: string;
  send: (message: EmailMessage) => Promise<EmailSendResult>;
};

export type EmailProviderId = 'resend' | 'brevo' | 'smtp' | 'console';
