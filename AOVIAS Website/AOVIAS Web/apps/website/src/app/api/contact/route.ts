import { NextResponse, type NextRequest } from 'next/server';
import { z } from 'zod';

import { companyLegal } from '@/config/company-legal';
import { verifyCaptcha } from '@/lib/contact/captcha';
import { checkRateLimit } from '@/lib/contact/rate-limit';
import {
  contactApiPayloadSchema,
  type ContactApiPayload,
} from '@/lib/contact/schema';
import {
  contactSubmissionFingerprint,
  createEmailService,
  deliverContactEmails,
  isDuplicateContactSubmission,
  rememberContactSubmission,
} from '@/lib/email';
import { emailLogger } from '@/lib/email/logger';

export const runtime = 'nodejs';
export const dynamic = 'force-dynamic';

const requestSchema = z.object({
  intent: z
    .enum(['contact', 'demo', 'enterprise', 'partner', 'newsletter'])
    .default('contact'),
  payload: z.record(z.string(), z.unknown()),
  captchaToken: z.string().optional(),
});

function clientIp(request: NextRequest): string {
  const forwarded = request.headers.get('x-forwarded-for');
  if (forwarded) {
    const first = forwarded.split(',')[0]?.trim();
    if (first) return first;
  }
  return (
    request.headers.get('x-real-ip') ||
    request.headers.get('cf-connecting-ip') ||
    'unknown'
  );
}

function asString(value: unknown): string {
  return typeof value === 'string' ? value.trim() : '';
}

function normalizePayload(
  intent: string,
  raw: Record<string, unknown>,
):
  | { ok: true; payload: ContactApiPayload }
  | { ok: false; message: string; fieldErrors?: Record<string, string> } {
  if (intent === 'contact') {
    const parsed = contactApiPayloadSchema.safeParse(raw);
    if (!parsed.success) {
      return {
        ok: false,
        message: 'Please check the form fields and try again.',
        fieldErrors: Object.fromEntries(
          parsed.error.issues.map((issue) => [
            issue.path.join('.') || 'form',
            issue.message,
          ]),
        ),
      };
    }
    return { ok: true, payload: parsed.data };
  }

  const name =
    asString(raw['name']) ||
    asString(raw['contactName']) ||
    'Website visitor';
  const email = asString(raw['email']);
  if (!email || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
    return {
      ok: false,
      message: 'A valid email address is required.',
      fieldErrors: { email: 'Enter a valid email address' },
    };
  }

  const organization =
    asString(raw['organization']) || asString(raw['org']) || '';
  const phone = asString(raw['phone']);
  const subject =
    asString(raw['subject']) ||
    asString(raw['productInterest']) ||
    `${intent} enquiry`;
  const message =
    asString(raw['message']) ||
    asString(raw['requirements']) ||
    asString(raw['projectSummary']) ||
    asString(raw['details']) ||
    JSON.stringify(raw, null, 2);

  const parsed = contactApiPayloadSchema.safeParse({
    name,
    email,
    organization,
    phone,
    subject: subject.slice(0, 160) || 'Website enquiry',
    message: message.length >= 10 ? message.slice(0, 2000) : `${message}\n—`,
    consent: true,
    company_website: asString(raw['company_website']),
  });

  if (!parsed.success) {
    return {
      ok: false,
      message: 'Please check the form fields and try again.',
    };
  }

  return { ok: true, payload: parsed.data };
}

/**
 * POST /api/contact — validate enquiry and deliver via EmailService.
 * SMTP / provider credentials never leave the server.
 */
export async function POST(request: NextRequest): Promise<NextResponse> {
  let json: unknown;
  try {
    json = await request.json();
  } catch {
    return NextResponse.json(
      { ok: false, message: 'Invalid request body.' },
      { status: 400 },
    );
  }

  const parsed = requestSchema.safeParse(json);
  if (!parsed.success) {
    return NextResponse.json(
      { ok: false, message: 'Invalid contact request.' },
      { status: 400 },
    );
  }

  const { intent, payload: rawPayload, captchaToken } = parsed.data;
  const normalized = normalizePayload(intent, rawPayload);
  if (!normalized.ok) {
    return NextResponse.json(
      {
        ok: false,
        message: normalized.message,
        ...(normalized.fieldErrors
          ? { fieldErrors: normalized.fieldErrors }
          : {}),
      },
      { status: 400 },
    );
  }

  const payload = normalized.payload;

  // Honeypot — treat as success without sending (silent discard).
  if (payload.company_website && payload.company_website.trim().length > 0) {
    return NextResponse.json({
      ok: true,
      submissionId: `HP-${Date.now().toString(36).toUpperCase()}`,
      message:
        'Thank you for contacting AOVIAS. Our team has received your enquiry and will respond shortly.',
    });
  }

  const fingerprint = contactSubmissionFingerprint(payload, intent);
  if (isDuplicateContactSubmission(fingerprint)) {
    emailLogger.warn('contact.duplicate', {
      intent,
      email: payload.email,
    });
    return NextResponse.json({
      ok: true,
      submissionId: `DUP-${fingerprint.slice(0, 10).toUpperCase()}`,
      message:
        'Thank you for contacting AOVIAS. Our team has received your enquiry and will respond shortly.',
      followUp: 'We typically respond within one business day.',
    });
  }

  const ip = clientIp(request);
  const rate = checkRateLimit(`contact:${ip}`);
  if (!rate.allowed) {
    const headers: HeadersInit | undefined = rate.retryAfterSec
      ? { 'Retry-After': String(rate.retryAfterSec) }
      : undefined;
    return NextResponse.json(
      {
        ok: false,
        message:
          'Too many messages were sent from this network. Please try again later.',
        retryable: true,
      },
      headers ? { status: 429, headers } : { status: 429 },
    );
  }

  const captcha = await verifyCaptcha({
    ...(captchaToken ? { token: captchaToken } : {}),
    remoteIp: ip,
  });
  if (!captcha.ok) {
    return NextResponse.json(
      {
        ok: false,
        message: captcha.error || 'CAPTCHA verification failed.',
        retryable: true,
      },
      { status: 400 },
    );
  }

  const timestamp = new Date().toISOString();
  const userAgent = request.headers.get('user-agent') || undefined;

  try {
    const emailService = createEmailService();
    const delivery = await deliverContactEmails(emailService, payload, {
      intent,
      timestamp,
      ip,
      ...(userAgent ? { userAgent } : {}),
    });

    if (!delivery.ok) {
      emailLogger.error('contact.delivery.failed', {
        provider: emailService.providerId,
        error: delivery.error,
      });
      return NextResponse.json(
        {
          ok: false,
          message: `We could not deliver your message right now. Please email ${companyLegal.enquiryEmail} or try again shortly.`,
          retryable: true,
        },
        { status: 502 },
      );
    }

    rememberContactSubmission(fingerprint);

    return NextResponse.json({
      ok: true,
      submissionId: delivery.submissionId,
      message:
        'Thank you for contacting AOVIAS. Our team has received your enquiry and will respond shortly.',
      followUp: 'We typically respond within one business day.',
    });
  } catch (error) {
    emailLogger.error('contact.delivery.exception', {
      error: error instanceof Error ? error.message : 'unknown',
    });
    return NextResponse.json(
      {
        ok: false,
        message: `We could not deliver your message right now. Please email ${companyLegal.enquiryEmail} or try again shortly.`,
        retryable: true,
      },
      { status: 502 },
    );
  }
}
