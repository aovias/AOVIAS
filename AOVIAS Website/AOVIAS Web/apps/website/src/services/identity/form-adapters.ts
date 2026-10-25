import type { FormSubmitHandler, FormSubmitResult } from '@/features/forms';
import { getIdentityClient } from '@/registries/sdk-registry';
import type { AuthenticationResult } from '@/services/identity/types';

function toFormResult(result: AuthenticationResult): FormSubmitResult {
  if (result.ok) {
    return {
      ok: true,
      ...(result.message ? { message: result.message } : {}),
    };
  }
  return {
    ok: false,
    message: result.message ?? 'Something went wrong. Please try again.',
    ...(result.retryable !== undefined ? { retryable: result.retryable } : {}),
  };
}

/** Bridge IdentityClient → FormEngine submit contract. */
export function createSignInSubmitHandler(options?: {
  redirectTo?: string;
  csrfToken?: string;
}): FormSubmitHandler<{
  email: string;
  password: string;
  rememberMe?: boolean;
}> {
  return async (values) => {
    const result = await getIdentityClient().signIn({
      email: values.email,
      password: values.password,
      ...(values.rememberMe !== undefined
        ? { rememberMe: values.rememberMe }
        : {}),
      ...(options?.redirectTo ? { redirectTo: options.redirectTo } : {}),
      ...(options?.csrfToken ? { csrfToken: options.csrfToken } : {}),
    });
    return toFormResult(result);
  };
}

export function createSignUpSubmitHandler(options?: {
  csrfToken?: string;
}): FormSubmitHandler<{
  name: string;
  email: string;
  password: string;
  confirmPassword: string;
  acceptedTerms: boolean;
}> {
  return async (values) => {
    const result = await getIdentityClient().signUp({
      name: values.name,
      email: values.email,
      password: values.password,
      acceptedTerms: values.acceptedTerms,
      ...(options?.csrfToken ? { csrfToken: options.csrfToken } : {}),
    });
    return toFormResult(result);
  };
}

export function createForgotPasswordSubmitHandler(options?: {
  csrfToken?: string;
}): FormSubmitHandler<{ email: string }> {
  return async (values) => {
    const result = await getIdentityClient().requestPasswordReset({
      email: values.email,
      ...(options?.csrfToken ? { csrfToken: options.csrfToken } : {}),
    });
    return toFormResult(result);
  };
}

export function createResetPasswordSubmitHandler(options: {
  token: string;
  csrfToken?: string;
}): FormSubmitHandler<{
  password: string;
  confirmPassword: string;
}> {
  return async (values) => {
    const result = await getIdentityClient().resetPassword({
      token: options.token,
      password: values.password,
      ...(options.csrfToken ? { csrfToken: options.csrfToken } : {}),
    });
    return toFormResult(result);
  };
}

export function createResendVerificationSubmitHandler(options?: {
  csrfToken?: string;
}): FormSubmitHandler<{ email: string }> {
  return async (values) => {
    const result = await getIdentityClient().resendVerification({
      email: values.email,
      ...(options?.csrfToken ? { csrfToken: options.csrfToken } : {}),
    });
    return toFormResult(result);
  };
}
