export const ROUTES = {
  HOME: '/',
  PRODUCTS: '/products',
  PRICING: '/pricing',
  CHECKOUT: '/checkout',
  COMPANY: '/company',
  CAREERS: '/company/careers',
  CONTACT: '/company/contact',
  SOLUTIONS: '/solutions',
  RESOURCES: '/resources',
  SEARCH: '/resources/search',
  LEGAL: '/legal',
  PRIVACY: '/legal/privacy',
  TERMS: '/legal/terms',
  COOKIES: '/legal/cookies',
  ACCESSIBILITY: '/legal/accessibility',
  SECURITY: '/legal/security',
  SIGN_IN: '/auth/sign-in',
  SIGN_UP: '/auth/sign-up',
  FORGOT_PASSWORD: '/auth/forgot-password',
  RESET_PASSWORD: '/auth/reset-password',
  VERIFY_EMAIL: '/auth/verify-email',
} as const;

/** Structural layout preview paths for Sprint 1.3 validation. */
export const LAYOUT_PREVIEWS = {
  marketing: '/',
  product: '/products/preview',
  documentation: '/resources/preview',
  legal: '/legal/preview',
  authentication: '/auth/sign-in',
  company: '/company/preview',
} as const;
