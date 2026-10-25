import { coreWebVitalsTargets, lighthouseTargets } from './performance';

/**
 * Lighthouse / LHCI assertion source of truth.
 * Consumed by `lighthouserc.cjs`, `scripts/lh-r1-4.mjs`, and documentation.
 */
export const lighthouseConfig = {
  targets: lighthouseTargets,
  coreWebVitals: {
    'largest-contentful-paint': coreWebVitalsTargets.lcpMs,
    'cumulative-layout-shift': coreWebVitalsTargets.cls,
    'interaction-to-next-paint': coreWebVitalsTargets.inpMs,
    'first-contentful-paint': coreWebVitalsTargets.fcpMs,
  },
  /** Critical Release 1 routes for production hardening gates. */
  criticalRoutes: [
    '/',
    '/company',
    '/products',
    '/products/command',
    '/products/studio',
    '/pricing',
    '/company/contact',
    '/resources',
    '/company/careers',
    '/legal/privacy',
    '/auth/sign-in',
  ] as const,
  /** Mobile form-factor spot checks (desktop covers the full critical set). */
  mobileSpotRoutes: ['/', '/products', '/company/contact'] as const,
  /** noindex routes — SEO score exempt from hard SEO=100 gate. */
  seoExemptRoutes: ['/auth/sign-in', '/checkout', '/checkout/cart'] as const,
  /** Expanded marketing registry (sitemap-aligned). */
  auditUrls: [
    '/',
    '/company',
    '/products',
    '/products/command',
    '/products/studio',
    '/pricing',
    '/company/contact',
    '/company/careers',
    '/solutions',
    '/solutions/healthcare',
    '/solutions/government',
    '/solutions/education',
    '/solutions/enterprise',
    '/resources',
    '/resources/blog',
    '/resources/docs',
    '/resources/search',
    '/legal',
    '/legal/privacy',
    '/legal/terms',
    '/legal/cookies',
    '/legal/accessibility',
    '/legal/security',
  ] as const,
  formFactor: 'desktop' as const,
} as const;

export type LighthouseConfig = typeof lighthouseConfig;
