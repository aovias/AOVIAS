import { siteConfig } from './site';

/**
 * Centralized SEO configuration — organization, social, robots, sitemap registry.
 * No page should hardcode these values.
 */
export const seoConfig = {
  siteName: siteConfig.name,
  defaultTitle: siteConfig.name,
  titleTemplate: `%s | ${siteConfig.name}`,
  defaultDescription: siteConfig.description,
  locale: siteConfig.locale,
  language: siteConfig.language,

  /** Fallback social / OG image (relative to site origin). */
  defaultOgImage: {
    path: '/og/default.png',
    width: 1200,
    height: 630,
    alt: `${siteConfig.name} — enterprise software platform`,
  },

  /** Align with calibrated --background (R1.DS.2.1). */
  themeColor: {
    light: '#E8EDF3',
    dark: '#090C11',
  },

  twitter: {
    card: 'summary_large_image' as const,
    site: '@aovias',
    creator: '@aovias',
  },

  organization: {
    name: siteConfig.name,
    legalName: 'AOVIAS Private Limited',
    url: siteConfig.url,
    logoPath: '/brand/aovias-logo-light.png',
    description: siteConfig.description,
    sameAs: [
      'https://www.linkedin.com/company/aovias',
      'https://x.com/aovias',
    ] as const,
    contactEmail: 'admin@aovias.com',
  },

  website: {
    name: siteConfig.name,
    description: siteConfig.description,
    /** Future site-search target; framework-ready. */
    potentialAction: {
      targetTemplate: '/resources/search?q={search_term_string}',
      queryInput: 'required name=search_term_string',
    },
  },

  robots: {
    /** Paths always disallowed from crawling. */
    disallow: ['/dev/', '/api/', '/auth/', '/checkout/', '/internal/'] as const,
    /** Preview / internal shells — not public content. */
    previewDisallow: ['/*/preview', '/*/preview/'] as const,
    /** Future admin / console areas (pre-emptively disallowed). */
    adminDisallow: ['/admin/', '/console/'] as const,
  },

  /**
   * Search Console / ownership verification placeholders.
   * Set via env in production — never hardcode live tokens in source.
   */
  verification: {
    googleEnvKey: 'NEXT_PUBLIC_GOOGLE_SITE_VERIFICATION',
    bingEnvKey: 'NEXT_PUBLIC_BING_SITE_VERIFICATION',
  },

  /** Shared OG image paths for section defaults. */
  ogImages: {
    default: '/og/default.png',
    home: '/og/home.png',
    company: '/og/company.png',
    contact: '/og/contact.png',
    careers: '/og/careers.png',
    products: '/og/products.png',
    command: '/og/command.png',
    studio: '/og/studio.png',
    pricing: '/og/pricing.png',
    technology: '/og/technology.png',
    solutions: '/og/solutions.png',
    resources: '/og/resources.png',
    blog: '/og/blog.png',
    documentation: '/og/documentation.png',
  },

  /**
   * Public routes eligible for sitemap inclusion.
   * Register future public pages here — previews and /dev are excluded.
   */
  sitemap: {
    defaultChangeFrequency: 'weekly' as const,
    defaultPriority: 0.7,
    routes: [
      {
        path: '/',
        changeFrequency: 'weekly' as const,
        priority: 1,
      },
      {
        path: '/company',
        changeFrequency: 'monthly' as const,
        priority: 0.9,
      },
      {
        path: '/company/careers',
        changeFrequency: 'weekly' as const,
        priority: 0.85,
      },
      {
        path: '/company/contact',
        changeFrequency: 'monthly' as const,
        priority: 0.9,
      },
      {
        path: '/products',
        changeFrequency: 'weekly' as const,
        priority: 0.95,
      },
      {
        path: '/pricing',
        changeFrequency: 'weekly' as const,
        priority: 0.95,
      },
      {
        path: '/products/command',
        changeFrequency: 'monthly' as const,
        priority: 0.9,
      },
      {
        path: '/products/studio',
        changeFrequency: 'monthly' as const,
        priority: 0.9,
      },
      {
        path: '/products/sentinel',
        changeFrequency: 'monthly' as const,
        priority: 0.55,
      },
      {
        path: '/products/nexus',
        changeFrequency: 'monthly' as const,
        priority: 0.55,
      },
      {
        path: '/products/insight',
        changeFrequency: 'monthly' as const,
        priority: 0.55,
      },
      {
        path: '/products/flow',
        changeFrequency: 'monthly' as const,
        priority: 0.55,
      },
      {
        path: '/technology',
        changeFrequency: 'weekly' as const,
        priority: 0.9,
      },
      {
        path: '/technology/platform',
        changeFrequency: 'weekly' as const,
        priority: 0.9,
      },
      {
        path: '/technology/platform/overview',
        changeFrequency: 'monthly' as const,
        priority: 0.8,
      },
      {
        path: '/technology/platform/services',
        changeFrequency: 'monthly' as const,
        priority: 0.8,
      },
      {
        path: '/technology/platform/ecosystem',
        changeFrequency: 'monthly' as const,
        priority: 0.8,
      },
      {
        path: '/technology/architecture',
        changeFrequency: 'monthly' as const,
        priority: 0.85,
      },
      {
        path: '/technology/ai',
        changeFrequency: 'weekly' as const,
        priority: 0.9,
      },
      {
        path: '/technology/ai/principles',
        changeFrequency: 'monthly' as const,
        priority: 0.8,
      },
      {
        path: '/technology/ai/capabilities',
        changeFrequency: 'monthly' as const,
        priority: 0.8,
      },
      {
        path: '/technology/ai/human-in-the-loop',
        changeFrequency: 'monthly' as const,
        priority: 0.8,
      },
      {
        path: '/technology/ai/responsible',
        changeFrequency: 'monthly' as const,
        priority: 0.85,
      },
      {
        path: '/technology/ai/roadmap',
        changeFrequency: 'monthly' as const,
        priority: 0.75,
      },
      {
        path: '/technology/engineering',
        changeFrequency: 'weekly' as const,
        priority: 0.9,
      },
      {
        path: '/technology/engineering/architecture',
        changeFrequency: 'monthly' as const,
        priority: 0.85,
      },
      {
        path: '/technology/engineering/design-principles',
        changeFrequency: 'monthly' as const,
        priority: 0.8,
      },
      {
        path: '/technology/engineering/reliability',
        changeFrequency: 'monthly' as const,
        priority: 0.8,
      },
      {
        path: '/technology/engineering/scalability',
        changeFrequency: 'monthly' as const,
        priority: 0.8,
      },
      {
        path: '/technology/engineering/observability',
        changeFrequency: 'monthly' as const,
        priority: 0.8,
      },
      {
        path: '/technology/trust',
        changeFrequency: 'weekly' as const,
        priority: 0.9,
      },
      {
        path: '/technology/trust/privacy',
        changeFrequency: 'monthly' as const,
        priority: 0.85,
      },
      {
        path: '/technology/trust/identity',
        changeFrequency: 'monthly' as const,
        priority: 0.8,
      },
      {
        path: '/technology/trust/compliance',
        changeFrequency: 'monthly' as const,
        priority: 0.8,
      },
      {
        path: '/technology/trust/data-protection',
        changeFrequency: 'monthly' as const,
        priority: 0.8,
      },
      {
        path: '/technology/integrations',
        changeFrequency: 'weekly' as const,
        priority: 0.85,
      },
      {
        path: '/technology/integrations/apis',
        changeFrequency: 'monthly' as const,
        priority: 0.7,
      },
      {
        path: '/technology/developer',
        changeFrequency: 'monthly' as const,
        priority: 0.7,
      },
      {
        path: '/technology/roadmap',
        changeFrequency: 'monthly' as const,
        priority: 0.8,
      },
      {
        path: '/technology/why',
        changeFrequency: 'monthly' as const,
        priority: 0.85,
      },
      {
        path: '/solutions',
        changeFrequency: 'weekly' as const,
        priority: 0.9,
      },
      {
        path: '/solutions/healthcare',
        changeFrequency: 'monthly' as const,
        priority: 0.85,
      },
      {
        path: '/solutions/government',
        changeFrequency: 'monthly' as const,
        priority: 0.85,
      },
      {
        path: '/solutions/education',
        changeFrequency: 'monthly' as const,
        priority: 0.85,
      },
      {
        path: '/solutions/enterprise',
        changeFrequency: 'monthly' as const,
        priority: 0.85,
      },
      {
        path: '/resources',
        changeFrequency: 'weekly' as const,
        priority: 0.9,
      },
      {
        path: '/resources/blog',
        changeFrequency: 'weekly' as const,
        priority: 0.8,
      },
      {
        path: '/resources/insights',
        changeFrequency: 'weekly' as const,
        priority: 0.8,
      },
      {
        path: '/resources/docs',
        changeFrequency: 'weekly' as const,
        priority: 0.85,
      },
      {
        path: '/resources/library',
        changeFrequency: 'weekly' as const,
        priority: 0.8,
      },
      {
        path: '/resources/search',
        changeFrequency: 'monthly' as const,
        priority: 0.6,
      },
      {
        path: '/resources/categories',
        changeFrequency: 'monthly' as const,
        priority: 0.7,
      },
      {
        path: '/legal',
        changeFrequency: 'monthly' as const,
        priority: 0.85,
      },
      {
        path: '/legal/privacy',
        changeFrequency: 'yearly' as const,
        priority: 0.8,
      },
      {
        path: '/legal/terms',
        changeFrequency: 'yearly' as const,
        priority: 0.8,
      },
      {
        path: '/legal/cookies',
        changeFrequency: 'yearly' as const,
        priority: 0.75,
      },
      {
        path: '/legal/accessibility',
        changeFrequency: 'yearly' as const,
        priority: 0.8,
      },
      {
        path: '/legal/security',
        changeFrequency: 'yearly' as const,
        priority: 0.8,
      },
    ],
  },

  icons: {
    icon: [
      { url: '/icons/favicon.svg', type: 'image/svg+xml' },
      { url: '/icons/icon-32.png', sizes: '32x32', type: 'image/png' },
      { url: '/icons/icon-16.png', sizes: '16x16', type: 'image/png' },
    ],
    apple: [{ url: '/icons/icon-180.png', sizes: '180x180', type: 'image/png' }],
  },

  manifestPath: '/manifest.webmanifest',
} as const;

export type SeoConfig = typeof seoConfig;
