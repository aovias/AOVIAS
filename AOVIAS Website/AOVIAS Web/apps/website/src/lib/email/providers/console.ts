import type {
  EmailAddress,
  EmailMessage,
  EmailProvider,
  EmailSendResult,
} from '../types';

function formatAddress(address: EmailAddress): string {
  return address.name
    ? `${address.name} <${address.email}>`
    : address.email;
}

function toList(value: EmailMessage['to']): EmailAddress[] {
  if (Array.isArray(value)) return [...value];
  return [value];
}

/** Development / unconfigured fallback — logs payload, never fails delivery. */
export function createConsoleEmailProvider(): EmailProvider {
  return {
    id: 'console',
    async send(message: EmailMessage): Promise<EmailSendResult> {
      const recipients = toList(message.to)
        .map((item) => formatAddress(item))
        .join(', ');
      console.info('[email:console]', {
        provider: 'console',
        to: recipients,
        from: formatAddress(message.from),
        replyTo: message.replyTo
          ? formatAddress(message.replyTo)
          : undefined,
        subject: message.subject,
        text: message.text,
      });
      return { ok: true, id: `console-${Date.now().toString(36)}` };
    },
  };
}
