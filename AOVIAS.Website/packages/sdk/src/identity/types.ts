/**
 * Identity domain models — stable for Release 2 Identity Platform adapters.
 */

export type IdentityProvider = {
  id: string;
  name: string;
  protocol?: 'password' | 'oidc' | 'saml';
};

export type User = {
  id: string;
  email: string;
  name?: string;
  emailVerified?: boolean;
};

export type AuthenticationSession = {
  id: string;
  userId: string;
  handle?: string;
  expiresAt?: string;
  providerId?: string;
};

export type VerificationStatus =
  | 'pending'
  | 'verified'
  | 'expired'
  | 'invalid'
  | 'already-verified';

export type PasswordResetRequest = {
  email: string;
  requestId?: string;
};

export type PasswordResetConfirmation = {
  token: string;
  password: string;
};

export type AuthenticationResult = {
  ok: boolean;
  user?: User;
  session?: AuthenticationSession;
  verificationStatus?: VerificationStatus;
  message?: string;
  redirectTo?: string;
  retryable?: boolean;
};

export type AuthorizationState = {
  authenticated: boolean;
  user: User | null;
  roles: readonly string[];
  permissions: readonly string[];
};
