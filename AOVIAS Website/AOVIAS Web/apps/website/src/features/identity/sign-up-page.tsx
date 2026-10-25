'use client';

import { SignUpForm } from '@/components/forms';
import { AuthPageShell } from '@/features/identity/auth-page-shell';
import { createSignUpSubmitHandler } from '@/services/identity';

/**
 * Sign Up — Form Engine + Identity Platform adapter.
 */
export function SignUpPage() {
  return (
    <AuthPageShell
      title="Create account"
      description="Register for AOVIAS Identity when production Identity is connected. This public route is an evaluation shell for Release 1."
      links={[
        { href: '/auth/sign-in', label: 'Already have an account? Sign in' },
        { href: '/company/contact', label: 'Contact AOVIAS' },
      ]}
      footerNote="Release 1 uses a mock Identity adapter. Do not enter production credentials."
    >
      <SignUpForm onSubmit={createSignUpSubmitHandler()} />
    </AuthPageShell>
  );
}
