import type { FieldValues } from 'react-hook-form';

import type { FormSubmitHandler, FormSubmitResult } from '@/features/forms/engine/types';

/**
 * Shapes a future API / CRM failure into the Form Engine result contract.
 * No network calls in Sprint 2.4 — ready for later adapters.
 */
export function toFormSubmitFailure(
  error: unknown,
  fallback = 'We could not submit your request. Please try again.',
): FormSubmitResult {
  if (
    typeof error === 'object' &&
    error !== null &&
    'message' in error &&
    typeof (error as { message: unknown }).message === 'string'
  ) {
    return {
      ok: false,
      message: (error as { message: string }).message,
      retryable: true,
    };
  }

  return { ok: false, message: fallback, retryable: true };
}

/**
 * Local mock submitter for design-system QA — simulates latency and success.
 * Replace with CRM / Identity adapters without changing form UIs.
 */
export function createMockSubmitHandler<TValues extends FieldValues>(options?: {
  delayMs?: number;
  message?: string;
  followUp?: string;
  /** Force a submission error for error-state QA. */
  fail?: boolean | ((values: TValues) => boolean);
}): FormSubmitHandler<TValues> {
  const delayMs = options?.delayMs ?? 600;
  const message =
    options?.message ?? 'Thank you. Your request has been received.';
  const followUp =
    options?.followUp ??
    'A member of our team will respond during business hours.';

  return async (values) => {
    await new Promise((resolve) => {
      setTimeout(resolve, delayMs);
    });

    const shouldFail =
      typeof options?.fail === 'function'
        ? options.fail(values)
        : Boolean(options?.fail);

    if (shouldFail) {
      return {
        ok: false,
        message: 'Submission could not be completed. Please try again.',
        retryable: true,
      };
    }

    return { ok: true, message, followUp };
  };
}
