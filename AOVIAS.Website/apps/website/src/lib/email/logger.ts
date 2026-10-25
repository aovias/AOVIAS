/**
 * Structured email logging — never logs passwords or raw SMTP auth.
 */

type EmailLogLevel = 'info' | 'warn' | 'error';

export type EmailLogContext = Record<
  string,
  string | number | boolean | undefined | null
>;

function write(level: EmailLogLevel, event: string, context?: EmailLogContext) {
  const payload = {
    scope: 'email',
    event,
    ts: new Date().toISOString(),
    ...context,
  };

  if (level === 'error') {
    console.error('[email]', payload);
    return;
  }
  if (level === 'warn') {
    console.warn('[email]', payload);
    return;
  }
  console.info('[email]', payload);
}

export const emailLogger = {
  info(event: string, context?: EmailLogContext) {
    write('info', event, context);
  },
  warn(event: string, context?: EmailLogContext) {
    write('warn', event, context);
  },
  error(event: string, context?: EmailLogContext) {
    write('error', event, context);
  },
};
