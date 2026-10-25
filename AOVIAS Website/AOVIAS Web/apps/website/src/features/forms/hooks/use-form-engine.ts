'use client';

import { useCallback, useRef, useState } from 'react';
import {
  useForm,
  type DefaultValues,
  type FieldValues,
  type Path,
  type UseFormReturn,
} from 'react-hook-form';
import type { ZodType } from 'zod';

import { zodResolver } from '@/features/forms/adapters/zod-resolver';
import type {
  FormStatus,
  FormSubmitHandler,
  FormSubmitSuccess,
} from '@/features/forms/engine/types';

export type UseFormEngineOptions<TValues extends FieldValues> = {
  schema: ZodType<TValues>;
  defaultValues?: DefaultValues<TValues>;
  onSubmit: FormSubmitHandler<TValues>;
  /** Validate on blur by default for enterprise UX. */
  mode?: 'onBlur' | 'onChange' | 'onSubmit' | 'onTouched' | 'all';
};

export type UseFormEngineReturn<TValues extends FieldValues> = {
  form: UseFormReturn<TValues>;
  status: FormStatus;
  submitError: string | null;
  success: FormSubmitSuccess | null;
  isSubmitting: boolean;
  handleSubmit: (event?: React.BaseSyntheticEvent) => Promise<void>;
  retry: () => void;
  resetStatus: () => void;
};

/**
 * Shared form lifecycle: validation, duplicate-submit guard, success/error status.
 */
export function useFormEngine<TValues extends FieldValues>({
  schema,
  defaultValues,
  onSubmit,
  mode = 'onBlur',
}: UseFormEngineOptions<TValues>): UseFormEngineReturn<TValues> {
  const [status, setStatus] = useState<FormStatus>('idle');
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [success, setSuccess] = useState<FormSubmitSuccess | null>(null);
  const submittingRef = useRef(false);

  const form = useForm<TValues>({
    resolver: zodResolver(schema as never),
    mode,
    ...(defaultValues !== undefined ? { defaultValues } : {}),
  }) as UseFormReturn<TValues>;

  const resetStatus = useCallback(() => {
    setStatus('idle');
    setSubmitError(null);
    setSuccess(null);
  }, []);

  const retry = useCallback(() => {
    setStatus('idle');
    setSubmitError(null);
  }, []);

  const handleSubmit = form.handleSubmit(async (values) => {
    if (submittingRef.current) return;
    submittingRef.current = true;
    setStatus('submitting');
    setSubmitError(null);

    try {
      const result = await onSubmit(values);
      if (result.ok) {
        setSuccess(result);
        setStatus('success');
        form.reset(defaultValues);
        return;
      }

      setStatus('error');
      setSubmitError(result.message);
      if (result.fieldErrors) {
        for (const [name, message] of Object.entries(result.fieldErrors)) {
          form.setError(name as Path<TValues>, { type: 'server', message });
        }
      }
    } catch {
      setStatus('error');
      setSubmitError('Something went wrong. Please try again.');
    } finally {
      submittingRef.current = false;
    }
  });

  return {
    form,
    status,
    submitError,
    success,
    isSubmitting: status === 'submitting',
    handleSubmit,
    retry,
    resetStatus,
  };
}
