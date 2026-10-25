import { z } from 'zod';

import {
  buildInternalNotificationEmail,
  buildInternalNotificationSubject,
} from '@/lib/email/templates/contact-emails';

const phonePattern = /^[+]?[\d\s().-]{7,22}$/;

/** Shared server shape for contact-form payloads. */
export const contactApiPayloadSchema = z.object({
  name: z.string().trim().min(2).max(100),
  email: z.string().trim().pipe(z.email()).refine((value) => value.length <= 254, {
    message: 'Email is too long',
  }),
  organization: z.string().trim().max(120).optional().default(''),
  phone: z
    .string()
    .trim()
    .max(22)
    .optional()
    .default('')
    .refine((value) => value.length === 0 || phonePattern.test(value), {
      message: 'Enter a valid phone number',
    }),
  subject: z.string().trim().min(2).max(160),
  message: z.string().trim().min(10).max(2000),
  consent: z.literal(true),
  /** Honeypot — must remain empty. */
  company_website: z.string().max(200).optional().default(''),
});

export type ContactApiPayload = z.infer<typeof contactApiPayloadSchema>;

/** @deprecated Prefer buildInternalNotificationSubject */
export function buildContactEmailSubject(payload: ContactApiPayload): string {
  return buildInternalNotificationSubject(payload);
}

/** @deprecated Prefer buildInternalNotificationEmail */
export function buildContactEmailBodies(
  payload: ContactApiPayload,
  meta: {
    intent: string;
    timestamp: string;
    ip?: string;
    userAgent?: string;
  },
): { text: string; html: string } {
  const built = buildInternalNotificationEmail(payload, meta);
  return { text: built.text, html: built.html };
}
