export type { IdentityClient } from '@/services/identity/client';
export {
  createForgotPasswordSubmitHandler,
  createResendVerificationSubmitHandler,
  createResetPasswordSubmitHandler,
  createSignInSubmitHandler,
  createSignUpSubmitHandler,
} from '@/services/identity/form-adapters';
export { createMockIdentityClient } from '@/services/identity/mock-client';
export { getIdentityClient } from '@/registries/sdk-registry';
export { resolveSafeRedirect } from '@/services/identity/redirects';
export type {
  AuthenticationResult,
  AuthenticationSession,
  AuthorizationState,
  IdentityProvider,
  PasswordResetConfirmation,
  PasswordResetRequest,
  User,
  VerificationStatus,
} from '@/services/identity/types';

