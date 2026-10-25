import type { WithSdkInfo } from '../core/client';
import type {
  AuthenticationResult,
  AuthorizationState,
  PasswordResetConfirmation,
  PasswordResetRequest,
  User,
  VerificationStatus,
} from './types';

/**
 * AOVIAS Identity Platform client contract.
 * Pages never call HTTP — only this interface via the registry.
 */
export type IdentityClient = WithSdkInfo & {
  signIn: (input: {
    email: string;
    password: string;
    rememberMe?: boolean;
    redirectTo?: string;
    csrfToken?: string;
  }) => Promise<AuthenticationResult>;

  signUp: (input: {
    name: string;
    email: string;
    password: string;
    acceptedTerms: boolean;
    csrfToken?: string;
  }) => Promise<AuthenticationResult>;

  requestPasswordReset: (
    input: PasswordResetRequest & { csrfToken?: string },
  ) => Promise<AuthenticationResult>;

  resetPassword: (
    input: PasswordResetConfirmation & { csrfToken?: string },
  ) => Promise<AuthenticationResult>;

  verifyEmail: (input: {
    token: string;
  }) => Promise<AuthenticationResult & { status: VerificationStatus }>;

  resendVerification: (input: {
    email: string;
    csrfToken?: string;
  }) => Promise<AuthenticationResult>;

  getCurrentUser: () => Promise<User | null>;

  getSession: () => Promise<AuthenticationResult['session'] | null>;

  getAuthenticationStatus: () => Promise<{
    authenticated: boolean;
    user: User | null;
  }>;

  getAuthorizationState: () => Promise<AuthorizationState>;
};
