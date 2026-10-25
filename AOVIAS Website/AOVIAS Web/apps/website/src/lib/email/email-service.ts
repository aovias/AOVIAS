import type {
  EmailMessage,
  EmailProvider,
  EmailSendResult,
} from '@/lib/email/types';
import { emailLogger } from '@/lib/email/logger';
import { withEmailRetry, type RetryOptions } from '@/lib/email/retry';

export type EmailServiceOptions = {
  provider: EmailProvider;
  retry: RetryOptions;
};

export type EmailServiceSendOptions = {
  /** Logical mail kind for logs (notification | confirmation | …). */
  kind?: string;
  /** Skip retry for this send (e.g. confirmation after critical path). */
  skipRetry?: boolean;
};

/**
 * Provider-agnostic EmailService.
 * Swap SMTP → Resend by changing EMAIL_PROVIDER; Contact API stays unchanged.
 */
export class EmailService {
  private readonly provider: EmailProvider;
  private readonly retry: RetryOptions;

  constructor(options: EmailServiceOptions) {
    this.provider = options.provider;
    this.retry = options.retry;
  }

  get providerId(): string {
    return this.provider.id;
  }

  async send(
    message: EmailMessage,
    options: EmailServiceSendOptions = {},
  ): Promise<EmailSendResult> {
    const kind = options.kind ?? 'message';
    const toSummary = Array.isArray(message.to)
      ? message.to.map((item) => item.email).join(',')
      : message.to.email;

    emailLogger.info('send.start', {
      provider: this.provider.id,
      kind,
      to: toSummary,
      subject: message.subject,
    });

    const execute = async (attempt: number): Promise<EmailSendResult> => {
      const result = await this.provider.send(message);
      if (!result.ok) {
        const error = new Error(result.error || 'Email delivery failed');
        (error as Error & { result: EmailSendResult }).result = result;
        throw error;
      }
      emailLogger.info('send.success', {
        provider: this.provider.id,
        kind,
        attempt,
        messageId: result.id,
        to: toSummary,
      });
      return result;
    };

    try {
      if (options.skipRetry) {
        return await execute(1);
      }

      return await withEmailRetry(execute, {
        ...this.retry,
        onRetry: (error, attempt, delayMs) => {
          emailLogger.warn('send.retry', {
            provider: this.provider.id,
            kind,
            attempt,
            delayMs,
            error: error instanceof Error ? error.message : 'unknown',
          });
        },
      });
    } catch (error) {
      const result =
        error &&
        typeof error === 'object' &&
        'result' in error &&
        (error as { result?: EmailSendResult }).result
          ? (error as { result: EmailSendResult }).result
          : {
              ok: false as const,
              error:
                error instanceof Error
                  ? error.message
                  : 'Email delivery failed',
            };

      emailLogger.error('send.failed', {
        provider: this.provider.id,
        kind,
        to: toSummary,
        error: result.error,
      });

      return result;
    }
  }
}
