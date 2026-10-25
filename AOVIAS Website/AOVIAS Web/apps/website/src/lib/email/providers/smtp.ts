import nodemailer from 'nodemailer';

import type {
  EmailAddress,
  EmailMessage,
  EmailProvider,
  EmailSendResult,
} from '../types';

export type SmtpConfig = {
  host: string;
  port: number;
  secure: boolean;
  requireTls?: boolean;
  user?: string;
  pass?: string;
  connectionTimeoutMs?: number;
  socketTimeoutMs?: number;
  greetingTimeoutMs?: number;
};

function toList(value: EmailMessage['to']): EmailAddress[] {
  if (Array.isArray(value)) return [...value];
  return [value];
}

function formatAddress(address: {
  email: string;
  name?: string;
}): string {
  return address.name
    ? `"${address.name.replaceAll('"', '')}" <${address.email}>`
    : address.email;
}

/**
 * SMTP email provider (Hostinger Mail and compatible SMTP servers).
 * Credentials must be supplied from server env — never from client code.
 */
export function createSmtpEmailProvider(config: SmtpConfig): EmailProvider {
  return {
    id: 'smtp',
    async send(message): Promise<EmailSendResult> {
      const transport = nodemailer.createTransport({
        host: config.host,
        port: config.port,
        secure: config.secure,
        ...(config.requireTls ? { requireTLS: true } : {}),
        ...(config.connectionTimeoutMs
          ? { connectionTimeout: config.connectionTimeoutMs }
          : {}),
        ...(config.socketTimeoutMs
          ? { socketTimeout: config.socketTimeoutMs }
          : {}),
        ...(config.greetingTimeoutMs
          ? { greetingTimeout: config.greetingTimeoutMs }
          : {}),
        ...(config.user
          ? {
              auth: {
                user: config.user,
                pass: config.pass ?? '',
              },
            }
          : {}),
      });

      try {
        const info = await transport.sendMail({
          from: formatAddress(message.from),
          to: toList(message.to).map(formatAddress).join(', '),
          subject: message.subject,
          text: message.text,
          ...(message.html ? { html: message.html } : {}),
          ...(message.replyTo
            ? { replyTo: formatAddress(message.replyTo) }
            : {}),
        });
        return {
          ok: true,
          ...(info.messageId ? { id: info.messageId } : {}),
        };
      } catch (error) {
        return {
          ok: false,
          error:
            error instanceof Error
              ? error.message
              : 'SMTP delivery failed',
        };
      } finally {
        transport.close();
      }
    },
  };
}
