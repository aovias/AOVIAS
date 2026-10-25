export const identitySeo = {
  signIn: {
    title: 'Sign in',
    description: 'Sign in to AOVIAS Identity to access Studio, Command, and related products.',
    path: '/auth/sign-in',
  },
  signUp: {
    title: 'Create account',
    description: 'Create an AOVIAS Identity account for institutional product access.',
    path: '/auth/sign-up',
  },
  forgotPassword: {
    title: 'Forgot password',
    description: 'Request a password reset for your AOVIAS Identity account.',
    path: '/auth/forgot-password',
  },
  resetPassword: {
    title: 'Reset password',
    description: 'Choose a new password for your AOVIAS Identity account.',
    path: '/auth/reset-password',
  },
  verifyEmail: {
    title: 'Verify email',
    description: 'Verify your email address for AOVIAS Identity.',
    path: '/auth/verify-email',
  },
} as const;
