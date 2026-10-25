import { getHostingerEmailSettings } from '@/config/email/hostinger';
import { EmailService } from '@/lib/email/email-service';
import { createBrevoEmailProvider } from '@/lib/email/providers/brevo';
import { createConsoleEmailProvider } from '@/lib/email/providers/console';
import { createResendEmailProvider } from '@/lib/email/providers/resend';
import { createSmtpEmailProvider } from '@/lib/email/providers/smtp';
import type { EmailProvider, EmailProviderId } from '@/lib/email/types';
import { emailLogger } from '@/lib/email/logger';

function readProviderId(): EmailProviderId {
  const raw = (process.env['EMAIL_PROVIDER'] ?? 'console').toLowerCase();
  if (
    raw === 'resend' ||
    raw === 'brevo' ||
    raw === 'smtp' ||
    raw === 'console'
  ) {
    return raw;
  }
  emailLogger.warn('provider.unknown', { value: raw });
  return 'console';
}

function readSmtpPassword(): string | undefined {
  return (
    process.env['SMTP_PASSWORD'] ||
    process.env['SMTP_PASS'] ||
    undefined
  );
}

function createSmtpFromEnv(): EmailProvider {
  const hostinger = getHostingerEmailSettings();
  const host = process.env['SMTP_HOST']?.trim() || hostinger.smtp.host;
  const portRaw = process.env['SMTP_PORT'];
  const port = Number(
    portRaw && portRaw.length > 0
      ? portRaw
      : hostinger.smtp.recommended.port,
  );
  const secureEnv = process.env['SMTP_SECURE'];
  const secure =
    secureEnv !== undefined && secureEnv.length > 0
      ? secureEnv === 'true'
      : hostinger.smtp.recommended.secure;

  const user = process.env['SMTP_USER']?.trim();
  const pass = readSmtpPassword();

  if (!user || !pass) {
    emailLogger.warn('smtp.auth.incomplete', {
      hasUser: Boolean(user),
      hasPassword: Boolean(pass),
    });
  }

  emailLogger.info('smtp.configured', {
    host,
    port: Number.isFinite(port) ? port : hostinger.smtp.recommended.port,
    secure,
    userConfigured: Boolean(user),
  });

  return createSmtpEmailProvider({
    host,
    port: Number.isFinite(port) ? port : hostinger.smtp.recommended.port,
    secure,
    requireTls: !secure && hostinger.smtp.recommended.requireTls,
    ...(user ? { user } : {}),
    ...(pass ? { pass } : {}),
    connectionTimeoutMs: hostinger.defaults.connectionTimeoutMs,
    socketTimeoutMs: hostinger.defaults.socketTimeoutMs,
    greetingTimeoutMs: hostinger.defaults.greetingTimeoutMs,
  });
}

/**
 * Resolve the active email provider from server env.
 * Never import this module from client components.
 */
export function createEmailProvider(): EmailProvider {
  const id = readProviderId();

  if (id === 'resend') {
    const apiKey = process.env['RESEND_API_KEY'];
    if (!apiKey) {
      emailLogger.warn('provider.fallback', {
        requested: 'resend',
        reason: 'RESEND_API_KEY missing',
      });
      return createConsoleEmailProvider();
    }
    return createResendEmailProvider(apiKey);
  }

  if (id === 'brevo') {
    const apiKey = process.env['BREVO_API_KEY'];
    if (!apiKey) {
      emailLogger.warn('provider.fallback', {
        requested: 'brevo',
        reason: 'BREVO_API_KEY missing',
      });
      return createConsoleEmailProvider();
    }
    return createBrevoEmailProvider(apiKey);
  }

  if (id === 'smtp') {
    return createSmtpFromEnv();
  }

  return createConsoleEmailProvider();
}

/**
 * Reusable EmailService — Contact API should call this, not providers directly.
 * Switching EMAIL_PROVIDER=smtp → resend requires no Contact API changes.
 */
export function createEmailService(): EmailService {
  const hostinger = getHostingerEmailSettings();
  return new EmailService({
    provider: createEmailProvider(),
    retry: {
      maxAttempts: hostinger.retry.maxAttempts,
      initialDelayMs: hostinger.retry.initialDelayMs,
      maxDelayMs: hostinger.retry.maxDelayMs,
      factor: hostinger.retry.factor,
    },
  });
}

export type { EmailMessage, EmailProvider, EmailSendResult } from '@/lib/email/types';
export type { EmailService } from '@/lib/email/email-service';
export {
  deliverContactEmails,
  resolveContactFormTo,
  resolveContactMailFrom,
  contactSubmissionFingerprint,
  isDuplicateContactSubmission,
  rememberContactSubmission,
} from '@/lib/email/contact-delivery';
