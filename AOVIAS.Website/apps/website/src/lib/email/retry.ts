export type RetryOptions = {
  maxAttempts: number;
  initialDelayMs: number;
  maxDelayMs: number;
  factor: number;
  /** Return true when the failure is worth retrying. */
  shouldRetry?: (error: unknown, attempt: number) => boolean;
  onRetry?: (error: unknown, attempt: number, delayMs: number) => void;
};

function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => {
    setTimeout(resolve, ms);
  });
}

function defaultShouldRetry(error: unknown): boolean {
  if (!(error instanceof Error)) return true;
  const message = error.message.toLowerCase();
  if (message.includes('invalid login') || message.includes('auth')) {
    return false;
  }
  if (message.includes('recipient') && message.includes('rejected')) {
    return false;
  }
  return true;
}

/**
 * Retry transient email/SMTP failures with exponential backoff.
 */
export async function withEmailRetry<T>(
  operation: (attempt: number) => Promise<T>,
  options: RetryOptions,
): Promise<T> {
  const {
    maxAttempts,
    initialDelayMs,
    maxDelayMs,
    factor,
    shouldRetry = defaultShouldRetry,
    onRetry,
  } = options;

  let lastError: unknown;

  for (let attempt = 1; attempt <= maxAttempts; attempt += 1) {
    try {
      return await operation(attempt);
    } catch (error) {
      lastError = error;
      const isLast = attempt >= maxAttempts;
      if (isLast || !shouldRetry(error, attempt)) {
        throw error;
      }
      const delay = Math.min(
        maxDelayMs,
        Math.round(initialDelayMs * factor ** (attempt - 1)),
      );
      onRetry?.(error, attempt, delay);
      await sleep(delay);
    }
  }

  throw lastError instanceof Error
    ? lastError
    : new Error('Email operation failed after retries');
}
