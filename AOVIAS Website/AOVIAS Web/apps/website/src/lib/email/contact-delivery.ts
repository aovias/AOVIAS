import { createHash } from 'node:crypto';

import { companyLegal } from '@/config/company-legal';
import type { ContactApiPayload } from '@/lib/contact/schema';
import type { EmailService } from '@/lib/email/email-service';
import { emailLogger } from '@/lib/email/logger';
import {
  buildInternalNotificationEmail,
  buildVisitorConfirmationEmail,
  type ContactMailMeta,
} from '@/lib/email/templates/contact-emails';
import type { EmailAddress, EmailSendResult } from '@/lib/email/types';

export type ContactMailDeliveryResult = {
  ok: boolean;
  submissionId?: string;
  notification?: EmailSendResult;
  confirmation?: EmailSendResult;
  error?: string;
  /** Internal notification failed — user should retry. */
  retryable?: boolean;
};

function parseFromAddress(raw: string, fallbackEmail: string): EmailAddress {
  const fromMatch = raw.match(/^(.*)<([^>]+)>$/);
  if (fromMatch) {
    return {
      name: fromMatch[1]?.trim() || 'AOVIAS Website',
      email: fromMatch[2]?.trim() || fallbackEmail,
    };
  }
  return { name: 'AOVIAS Website', email: raw.trim() || fallbackEmail };
}

export function resolveContactMailFrom(): EmailAddress {
  const fromEmail =
    process.env['EMAIL_FROM'] ||
    process.env['RESEND_FROM'] ||
    `AOVIAS Website <noreply@${new URL(companyLegal.website).hostname}>`;
  return parseFromAddress(fromEmail, companyLegal.enquiryEmail);
}

export function resolveContactFormTo(): EmailAddress {
  const to =
    process.env['CONTACT_FORM_TO']?.trim() || companyLegal.enquiryEmail;
  return { email: to, name: 'AOVIAS Enquiries' };
}

/** Stable fingerprint for short-window duplicate detection (server-side). */
export function contactSubmissionFingerprint(
  payload: ContactApiPayload,
  intent: string,
): string {
  const material = [
    intent,
    payload.email.toLowerCase(),
    payload.subject.trim().toLowerCase(),
    payload.message.trim().toLowerCase(),
  ].join('|');
  return createHash('sha256').update(material).digest('hex').slice(0, 32);
}

const recentSubmissions = new Map<string, number>();
const DUPLICATE_WINDOW_MS = 2 * 60 * 1000;

export function isDuplicateContactSubmission(fingerprint: string): boolean {
  const now = Date.now();
  for (const [key, at] of recentSubmissions) {
    if (now - at > DUPLICATE_WINDOW_MS) recentSubmissions.delete(key);
  }
  const previous = recentSubmissions.get(fingerprint);
  return Boolean(previous && now - previous < DUPLICATE_WINDOW_MS);
}

export function rememberContactSubmission(fingerprint: string): void {
  recentSubmissions.set(fingerprint, Date.now());
}

/**
 * Send internal notification + visitor confirmation through EmailService.
 * Contact API depends on this helper — not on a specific SMTP/Resend provider.
 */
export async function deliverContactEmails(
  emailService: EmailService,
  payload: ContactApiPayload,
  meta: ContactMailMeta,
): Promise<ContactMailDeliveryResult> {
  const from = resolveContactMailFrom();
  const to = resolveContactFormTo();
  const notification = buildInternalNotificationEmail(payload, meta);
  const confirmation = buildVisitorConfirmationEmail(payload, meta);

  emailLogger.info('contact.delivery.start', {
    provider: emailService.providerId,
    intent: meta.intent,
    to: to.email,
    visitor: payload.email,
  });

  const notificationResult = await emailService.send(
    {
      to,
      from,
      replyTo: { email: payload.email, name: payload.name },
      subject: notification.subject,
      text: notification.text,
      html: notification.html,
    },
    { kind: 'contact.notification' },
  );

  if (!notificationResult.ok) {
    const error =
      notificationResult.error ?? 'Internal notification delivery failed';
    return {
      ok: false,
      notification: notificationResult,
      error,
      retryable: true,
    };
  }

  const confirmationResult = await emailService.send(
    {
      to: { email: payload.email, name: payload.name },
      from,
      subject: confirmation.subject,
      text: confirmation.text,
      html: confirmation.html,
    },
    { kind: 'contact.confirmation' },
  );

  if (!confirmationResult.ok) {
    // Internal mail succeeded — treat as success; log confirmation failure.
    emailLogger.warn('contact.confirmation.failed', {
      provider: emailService.providerId,
      visitor: payload.email,
      error: confirmationResult.error,
    });
  }

  return {
    ok: true,
    submissionId:
      notificationResult.id ||
      confirmationResult.id ||
      `CNT-${Date.now().toString(36).toUpperCase()}`,
    notification: notificationResult,
    confirmation: confirmationResult,
  };
}
