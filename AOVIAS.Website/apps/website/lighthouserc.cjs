/**
 * Lighthouse CI configuration — R1.4 production hardening gates.
 * Run: npm run lighthouse --workspace=@aovias/website
 * (requires a running production server on PORT, default 3000)
 *
 * Targets mirror src/config/lighthouse.ts / performance.ts.
 * Auth/checkout SEO < 100 is expected (noindex) — audited separately via lh-r1-4.mjs.
 */

const PORT = process.env.PORT || '3000';
const BASE = `http://127.0.0.1:${PORT}`;

module.exports = {
  ci: {
    collect: {
      url: [
        `${BASE}/`,
        `${BASE}/company`,
        `${BASE}/products`,
        `${BASE}/products/command`,
        `${BASE}/products/studio`,
        `${BASE}/pricing`,
        `${BASE}/company/contact`,
        `${BASE}/resources`,
        `${BASE}/company/careers`,
        `${BASE}/legal/privacy`,
      ],
      numberOfRuns: 1,
      settings: {
        preset: 'desktop',
      },
    },
    assert: {
      assertions: {
        'categories:performance': ['error', { minScore: 0.95 }],
        'categories:accessibility': ['error', { minScore: 1 }],
        'categories:best-practices': ['error', { minScore: 1 }],
        'categories:seo': ['error', { minScore: 1 }],
        'largest-contentful-paint': ['warn', { maxNumericValue: 2500 }],
        'cumulative-layout-shift': ['error', { maxNumericValue: 0.1 }],
        'first-contentful-paint': ['warn', { maxNumericValue: 1800 }],
      },
    },
    upload: {
      target: 'filesystem',
      outputDir: './.lighthouseci',
    },
  },
};
