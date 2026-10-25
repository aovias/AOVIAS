'use client';

import { SignInForm } from '@/components/forms';
import { AuthPageShell } from '@/features/identity/auth-page-shell';
import { createSignInSubmitHandler } from '@/services/identity';

export type SignInPageProps = {
  redirectTo?: string;
};

/**
 * Sign In — Form Engine + Identity Platform adapter (mock in Release 1).
 */
export function SignInPage({ redirectTo }: SignInPageProps) {
  return (
    <AuthPageShell
      title="Sign in"
      description="Identity authentication for AOVIAS Studio and Command. This public website route is an evaluation shell — production Identity is not connected yet."
      links={[
        { href: '/auth/forgot-password', label: 'Forgot password?' },
        { href: '/auth/sign-up', label: 'Create an account' },
        { href: '/company/contact', label: 'Contact AOVIAS' },
      ]}
      footerNote="Release 1 uses a mock Identity adapter for integration readiness. Do not enter production credentials."
    >
      <SignInForm
        onSubmit={createSignInSubmitHandler({
          ...(redirectTo ? { redirectTo } : {}),
        })}
      />
    </AuthPageShell>
  );
}
