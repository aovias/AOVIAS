/**
 * Centralized performance budgets, Core Web Vitals targets, and caching policy.
 * Enforce via tooling scripts — do not hardcode budgets in feature code.
 */

export const coreWebVitalsTargets = {
  /** Largest Contentful Paint */
  lcpMs: 2500,
  /** Interaction to Next Paint */
  inpMs: 200,
  /** Cumulative Layout Shift */
  cls: 0.1,
  /** First Contentful Paint */
  fcpMs: 1800,
  /** Time to First Byte */
  ttfbMs: 800,
} as const;

export const lighthouseTargets = {
  performance: 95,
  accessibility: 100,
  'best-practices': 100,
  seo: 100,
} as const;

/** Gzip-oriented JS budgets (KB). Aligns with Performance Engineering Standards. */
export const performanceBudgets = {
  /** Shared + route first-load JS (First Load JS from Next build). */
  initialJsKbGzip: 150,
  /** Additional per-route JS beyond shared. */
  routeJsKbGzip: 75,
  /** Approximate CSS transfer budget. */
  cssKbGzip: 50,
  /** Font transfer budget (all faces). */
  fontsKb: 120,
  /** Above-the-fold image budget. */
  heroImageKb: 200,
  /** Third-party script budget (should stay near zero on marketing). */
  thirdPartyKbGzip: 30,
} as const;

/**
 * Cache-Control policies for future CDN / edge deployment.
 * Applied via `next.config` headers where applicable.
 */
export const cachePolicies = {
  /** Hashed Next static assets — immutable. */
  nextStatic: 'public, max-age=31536000, immutable',
  /** Optimized image responses. */
  images: 'public, max-age=86400, stale-while-revalidate=604800',
  /** Public brand/media assets. */
  publicAssets: 'public, max-age=86400, stale-while-revalidate=604800',
  /** HTML / RSC payloads — short cache, revalidate. */
  documents: 'public, max-age=0, s-maxage=60, stale-while-revalidate=300',
  /** Metadata routes (sitemap/robots) — moderate. */
  metadata: 'public, max-age=3600, stale-while-revalidate=86400',
  /** Fonts served from Google via next/font are self-hosted at build — treat as static. */
  fonts: 'public, max-age=31536000, immutable',
} as const;

export const imageDefaults = {
  formats: ['image/avif', 'image/webp'] as const,
  deviceSizes: [640, 750, 828, 1080, 1200, 1920, 2048],
  imageSizes: [16, 32, 48, 64, 96, 128, 256, 384],
  minimumCacheTTL: 60 * 60 * 24,
  /** Default sizes attribute for content-width images. */
  contentSizes: '(max-width: 768px) 100vw, (max-width: 1280px) 80vw, 1200px',
  heroSizes: '100vw',
  thumbnailSizes: '(max-width: 768px) 50vw, 256px',
} as const;

export const performanceConfig = {
  coreWebVitals: coreWebVitalsTargets,
  lighthouse: lighthouseTargets,
  budgets: performanceBudgets,
  cache: cachePolicies,
  images: imageDefaults,
} as const;

export type PerformanceConfig = typeof performanceConfig;
