'use client';

import { useEffect, useState } from 'react';

import { ResendVerificationForm } from '@/components/forms';
import { EmptyState, ErrorState } from '@/components/system-states';
import { Stack, Text } from '@/components/primitives';
import { AuthPageShell } from '@/features/identity/auth-page-shell';
import {
  createResendVerificationSubmitHandler,
  getIdentityClient,
  type VerificationStatus,
} from '@/services/identity';

export type VerifyEmailPageProps = {
  token?: string;
};

type VerifyState =
  | { kind: 'idle' }
  | { kind: 'loading' }
  | { kind: 'success'; status: VerificationStatus; message: string }
  | { kind: 'failure'; status: VerificationStatus; message: string };

/**
 * Email Verification — Identity token status + resend form.
 */
export function VerifyEmailPage({ token }: VerifyEmailPageProps) {
  const [state, setState] = useState<VerifyState>(
    token ? { kind: 'loading' } : { kind: 'idle' },
  );

  useEffect(() => {
    if (!token) return;

    let cancelled = false;
    setState({ kind: 'loading' });

    void getIdentityClient().verifyEmail({ token }).then((result) => {
      if (cancelled) return;
      if (result.ok) {
        setState({
          kind: 'success',
          status: result.status,
          message: result.message ?? 'Email verified.',
        });
        return;
      }
      setState({
        kind: 'failure',
        status: result.status,
        message: result.message ?? 'Verification failed.',
      });
    });

    return () => {
      cancelled = true;
    };
  }, [token]);

  if (state.kind === 'loading') {
    return (
      <EmptyState
        title="Verifying email"
        description="Please wait while AOVIAS Identity confirms your address."
        variant="subtle"
        reveal={false}
      />
    );
  }

  if (state.kind === 'success') {
    return (
      <EmptyState
        title={
          state.status === 'already-verified'
            ? 'Already verified'
            : 'Email verified'
        }
        description={state.message}
        variant="default"
        reveal={false}
        primaryAction={{ label: 'Sign in', href: '/auth/sign-in' }}
      />
    );
  }

  if (state.kind === 'failure') {
    return (
      <Stack gap={8}>
        <ErrorState
          title="Verification unsuccessful"
          description={state.message}
          variant="generic"
          retryAction={{
            label: 'Request a new link',
            href: '/auth/verify-email',
          }}
          supportAction={{ label: 'Sign in', href: '/auth/sign-in' }}
          reveal={false}
        />
        <AuthPageShell
          title="Resend verification"
          description="Enter your work email to receive a new verification message."
        >
          <ResendVerificationForm
            onSubmit={createResendVerificationSubmitHandler()}
          />
        </AuthPageShell>
      </Stack>
    );
  }

  return (
    <AuthPageShell
      title="Verify your email"
      description="Open the verification link from your inbox, or resend a new message below."
      links={[{ href: '/auth/sign-in', label: 'Back to sign in' }]}
      footerNote="Verification protects institutional accounts."
    >
      <Stack gap={4}>
        <Text as="p" size="small" tone="muted">
          No verification token was provided in this link.
        </Text>
        <ResendVerificationForm
          onSubmit={createResendVerificationSubmitHandler()}
        />
      </Stack>
    </AuthPageShell>
  );
}
