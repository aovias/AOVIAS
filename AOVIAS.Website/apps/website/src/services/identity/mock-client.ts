import type { IdentityClient } from '@/services/identity/client';
import { resolveSafeRedirect } from '@/services/identity/redirects';
import type { AuthenticationResult } from '@/services/identity/types';

async function delay(ms = 500) {
  await new Promise((resolve) => {
    setTimeout(resolve, ms);
  });
}

/**
 * Local mock Identity Platform — replace with HTTP Identity adapters later.
 * Simulates latency and deterministic failure cues for QA.
 */
export function createMockIdentityClient(): IdentityClient {
  return {
    async signIn({ email, rememberMe, redirectTo }) {
      await delay();
      if (email.toLowerCase() === 'fail@aovias.com') {
        return {
          ok: false,
          message: 'Invalid email or password.',
          retryable: true,
        } satisfies AuthenticationResult;
      }
      return {
        ok: true,
        message: rememberMe
          ? 'Signed in. Your session will persist on this device.'
          : 'Signed in successfully.',
        user: {
          id: 'mock-user',
          email,
          emailVerified: true,
        },
        session: {
          id: 'mock-session',
          userId: 'mock-user',
          handle: 'mock-handle',
        },
        redirectTo: resolveSafeRedirect(redirectTo),
      };
    },

    async signUp({ name, email }) {
      await delay();
      if (email.toLowerCase() === 'taken@aovias.com') {
        return {
          ok: false,
          message: 'An account with this email already exists.',
          retryable: false,
        };
      }
      return {
        ok: true,
        message: 'Account created. Verify your email to continue.',
        user: {
          id: 'mock-user-new',
          email,
          name,
          emailVerified: false,
        },
        verificationStatus: 'pending',
        redirectTo: '/auth/verify-email',
      };
    },

    async requestPasswordReset({ email }) {
      await delay();
      // Always succeed — avoid account enumeration.
      return {
        ok: true,
        message:
          'If an account exists for that email, reset instructions were sent.',
        user: { id: 'unknown', email },
      };
    },

    async resetPassword({ token }) {
      await delay();
      if (!token || token === 'invalid') {
        return {
          ok: false,
          message: 'This reset link is invalid or has expired.',
          retryable: false,
        };
      }
      return {
        ok: true,
        message: 'Your password has been updated.',
        redirectTo: '/auth/sign-in',
      };
    },

    async verifyEmail({ token }) {
      await delay();
      if (!token) {
        return {
          ok: false,
          status: 'invalid',
          message: 'A verification token is required.',
          retryable: false,
        };
      }
      if (token === 'expired') {
        return {
          ok: false,
          status: 'expired',
          message: 'This verification link has expired.',
          retryable: true,
        };
      }
      if (token === 'invalid') {
        return {
          ok: false,
          status: 'invalid',
          message: 'This verification link is not valid.',
          retryable: false,
        };
      }
      if (token === 'already') {
        return {
          ok: true,
          status: 'already-verified',
          message: 'Your email is already verified.',
          redirectTo: '/auth/sign-in',
        };
      }
      return {
        ok: true,
        status: 'verified',
        message: 'Email verified successfully.',
        redirectTo: '/auth/sign-in',
      };
    },

    async resendVerification({ email }) {
      await delay();
      return {
        ok: true,
        message: `If ${email} needs verification, a new message was sent.`,
        verificationStatus: 'pending',
      };
    },

    async getCurrentUser() {
      return null;
    },

    async getSession() {
      return null;
    },

    async getAuthenticationStatus() {
      return { authenticated: false, user: null };
    },

    async getAuthorizationState() {
      return {
        authenticated: false,
        user: null,
        roles: [],
        permissions: [],
      };
    },

    getClientInfo() {
      return {
        name: 'identity',
        mode: 'mock' as const,
        version: '0.1.0',
      };
    },
  };
}

/** @deprecated Prefer getIdentityClient() from the SDK registry. */
export const identityClient = createMockIdentityClient();
