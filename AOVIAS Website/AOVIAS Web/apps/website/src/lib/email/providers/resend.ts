import type {
  EmailAddress,
  EmailMessage,
  EmailProvider,
  EmailSendResult,
} from '../types';

function toList(value: EmailMessage['to']): EmailAddress[] {
  if (Array.isArray(value)) return [...value];
  return [value];
}

/**
 * Resend HTTP API — https://resend.com/docs/api-reference/emails/send-email
 */
export function createResendEmailProvider(apiKey: string): EmailProvider {
  return {
    id: 'resend',
    async send(message): Promise<EmailSendResult> {
      const response = await fetch('https://api.resend.com/emails', {
        method: 'POST',
        headers: {
          Authorization: `Bearer ${apiKey}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          from: message.from.name
            ? `${message.from.name} <${message.from.email}>`
            : message.from.email,
          to: toList(message.to).map((item) => item.email),
          subject: message.subject,
          text: message.text,
          ...(message.html ? { html: message.html } : {}),
          ...(message.replyTo
            ? { reply_to: message.replyTo.email }
            : {}),
        }),
      });

      const body = (await response.json().catch(() => ({}))) as {
        id?: string;
        message?: string;
        name?: string;
      };

      if (!response.ok) {
        return {
          ok: false,
          error:
            body.message ||
            body.name ||
            `Resend error (${response.status})`,
        };
      }

      return { ok: true, ...(body.id ? { id: body.id } : {}) };
    },
  };
}
