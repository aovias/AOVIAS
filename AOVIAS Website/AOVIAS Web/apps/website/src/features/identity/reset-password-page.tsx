'use client';

import { ResetPasswordForm } from '@/components/forms';
import { ErrorState } from '@/components/system-states';
import { AuthPageShell } from '@/features/identity/auth-page-shell';
import { createResetPasswordSubmitHandler } from '@/services/identity';

export type ResetPasswordPageProps = {
  token?: string;
};

/**
 * Reset Password — token from Identity email link; Form Engine validation.
 */
export function ResetPasswordPage({ token }: ResetPasswordPageProps) {
  if (!token) {
    return (
      <ErrorState
        title="Reset link required"
        description="Open the password reset link from your email, or request a new one."
        variant="generic"
        retryAction={{
          label: 'Forgot password',
          href: '/auth/forgot-password',
        }}
        supportAction={{
          label: 'Sign in',
          href: '/auth/sign-in',
        }}
        reveal={false}
      />
    );
  }

  return (
    <AuthPageShell
      title="Reset password"
      description="Choose a new password for your AOVIAS Identity account."
      links={[{ href: '/auth/sign-in', label: 'Back to sign in' }]}
    >
      <ResetPasswordForm
        onSubmit={createResetPasswordSubmitHandler({ token })}
      />
    </AuthPageShell>
  );
}
