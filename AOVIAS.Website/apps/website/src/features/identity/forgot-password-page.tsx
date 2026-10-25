'use client';

import { ForgotPasswordForm } from '@/components/forms';
import { AuthPageShell } from '@/features/identity/auth-page-shell';
import { createForgotPasswordSubmitHandler } from '@/services/identity';

/**
 * Forgot Password — request reset via Identity Platform.
 */
export function ForgotPasswordPage() {
  return (
    <AuthPageShell
      title="Forgot password"
      description="Enter your work email. If an account exists, Identity will send reset instructions."
      links={[
        { href: '/auth/sign-in', label: 'Back to sign in' },
        { href: '/auth/sign-up', label: 'Create an account' },
      ]}
    >
      <ForgotPasswordForm onSubmit={createForgotPasswordSubmitHandler()} />
    </AuthPageShell>
  );
}
