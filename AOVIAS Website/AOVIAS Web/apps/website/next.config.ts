import bundleAnalyzer from '@next/bundle-analyzer';
import type { NextConfig } from 'next';

import { cachePolicies, imageDefaults } from './src/config/performance';

const withBundleAnalyzer = bundleAnalyzer({
  enabled: process.env['ANALYZE'] === 'true',
});

const isProd = process.env['NODE_ENV'] === 'production';

/**
 * Enforceable CSP baseline for Next.js App Router + next/font + next-themes.
 * Tighten iteratively (nonces, drop unsafe-inline) without breaking builds.
 */
function contentSecurityPolicy(): string {
  const directives = [
    "default-src 'self'",
    "base-uri 'self'",
    "object-src 'none'",
    "frame-ancestors 'none'",
    "form-action 'self'",
    "img-src 'self' data: blob: https:",
    "font-src 'self' data:",
    "style-src 'self' 'unsafe-inline'",
    // next-themes + Next runtime may require inline scripts; tighten with nonces later
    "script-src 'self' 'unsafe-inline' 'unsafe-eval'",
    "connect-src 'self'",
    // Product demos — official YouTube embeds
    "frame-src 'self' https://www.youtube.com https://www.youtube-nocookie.com",
    "child-src 'self' https://www.youtube.com https://www.youtube-nocookie.com",
    "worker-src 'self' blob:",
    "manifest-src 'self'",
  ];

  if (isProd) {
    directives.push('upgrade-insecure-requests');
  }

  return directives.join('; ');
}

function securityHeaders(): { key: string; value: string }[] {
  const headers: { key: string; value: string }[] = [
    { key: 'X-Content-Type-Options', value: 'nosniff' },
    { key: 'Referrer-Policy', value: 'strict-origin-when-cross-origin' },
    {
      key: 'Permissions-Policy',
      value: 'camera=(), microphone=(), geolocation=(), payment=(), usb=()',
    },
    { key: 'X-Frame-Options', value: 'DENY' },
    { key: 'Content-Security-Policy', value: contentSecurityPolicy() },
    { key: 'X-DNS-Prefetch-Control', value: 'on' },
  ];

  if (isProd) {
    headers.push({
      key: 'Strict-Transport-Security',
      value: 'max-age=63072000; includeSubDomains; preload',
    });
  }

  headers.push(
    { key: 'Cross-Origin-Opener-Policy', value: 'same-origin' },
    { key: 'Cross-Origin-Resource-Policy', value: 'same-origin' },
    { key: 'X-Permitted-Cross-Domain-Policies', value: 'none' },
  );

  return headers;
}

const nextConfig: NextConfig = {
  output: 'standalone',
  reactStrictMode: true,
  poweredByHeader: false,
  transpilePackages: [
    '@aovias/design-system',
    '@aovias/design-tokens',
    '@aovias/sdk',
  ],
  compress: true,
  compiler: {
    removeConsole:
      process.env['NODE_ENV'] === 'production'
        ? { exclude: ['error', 'warn'] }
        : false,
  },
  experimental: {
    optimizePackageImports: ['lucide-react', 'framer-motion'],
  },
  images: {
    formats: [...imageDefaults.formats],
    deviceSizes: [...imageDefaults.deviceSizes],
    imageSizes: [...imageDefaults.imageSizes],
    minimumCacheTTL: imageDefaults.minimumCacheTTL,
  },
  async redirects() {
    return [
      {
        source: '/technology/security',
        destination: '/technology/trust',
        permanent: true,
      },
      {
        source: '/technology/privacy',
        destination: '/technology/trust/privacy',
        permanent: true,
      },
      {
        source: '/technology/apis',
        destination: '/technology/integrations/apis',
        permanent: true,
      },
    ];
  },
  async headers() {
    return [
      {
        source: '/:path*',
        headers: securityHeaders(),
      },
      {
        source: '/_next/static/:path*',
        headers: [{ key: 'Cache-Control', value: cachePolicies.nextStatic }],
      },
      {
        source: '/_next/static/media/:path*',
        headers: [{ key: 'Cache-Control', value: cachePolicies.fonts }],
      },
      {
        source: '/_next/image',
        headers: [{ key: 'Cache-Control', value: cachePolicies.images }],
      },
      {
        source: '/images/:path*',
        headers: [{ key: 'Cache-Control', value: cachePolicies.publicAssets }],
      },
      {
        source: '/brand/:path*',
        headers: [{ key: 'Cache-Control', value: cachePolicies.publicAssets }],
      },
      {
        source: '/og/:path*',
        headers: [{ key: 'Cache-Control', value: cachePolicies.publicAssets }],
      },
      {
        source: '/icons/:path*',
        headers: [{ key: 'Cache-Control', value: cachePolicies.publicAssets }],
      },
      {
        source: '/sitemap.xml',
        headers: [{ key: 'Cache-Control', value: cachePolicies.metadata }],
      },
      {
        source: '/robots.txt',
        headers: [{ key: 'Cache-Control', value: cachePolicies.metadata }],
      },
    ];
  },
};

export default withBundleAnalyzer(nextConfig);
