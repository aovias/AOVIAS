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
 * Brevo transactional email API —
 * https://developers.brevo.com/reference/sendtransacemail
 */
export function createBrevoEmailProvider(apiKey: string): EmailProvider {
  return {
    id: 'brevo',
    async send(message): Promise<EmailSendResult> {
      const response = await fetch('https://api.brevo.com/v3/smtp/email', {
        method: 'POST',
        headers: {
          'api-key': apiKey,
          'Content-Type': 'application/json',
          Accept: 'application/json',
        },
        body: JSON.stringify({
          sender: {
            email: message.from.email,
            ...(message.from.name ? { name: message.from.name } : {}),
          },
          to: toList(message.to).map((item) => ({
            email: item.email,
            ...(item.name ? { name: item.name } : {}),
          })),
          subject: message.subject,
          textContent: message.text,
          ...(message.html ? { htmlContent: message.html } : {}),
          ...(message.replyTo
            ? {
                replyTo: {
                  email: message.replyTo.email,
                  ...(message.replyTo.name
                    ? { name: message.replyTo.name }
                    : {}),
                },
              }
            : {}),
        }),
      });

      const body = (await response.json().catch(() => ({}))) as {
        messageId?: string;
        message?: string;
      };

      if (!response.ok) {
        return {
          ok: false,
          error: body.message || `Brevo error (${response.status})`,
        };
      }

      return {
        ok: true,
        ...(body.messageId ? { id: body.messageId } : {}),
      };
    },
  };
}
