import fs from 'node:fs';
import path from 'node:path';
import { createRequire } from 'node:module';

const require = createRequire(import.meta.url);
const lighthouseModule = require('lighthouse');
const lighthouse = lighthouseModule.default ?? lighthouseModule;
const desktopConfig = lighthouseModule.desktopConfig;
const chromeLauncher = require('chrome-launcher');

const BASE = process.env.PORT
  ? `http://127.0.0.1:${process.env.PORT}`
  : 'http://127.0.0.1:3000';

/** Critical Release 1 routes — SEO hard-gate excludes auth/checkout (noindex). */
const desktopRoutes = [
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
];

const mobileSpotRoutes = ['/', '/products', '/company/contact'];

const SEO_EXEMPT = new Set(['/auth/sign-in', '/checkout', '/checkout/cart']);

const outDir = path.resolve('.lighthouseci-r1-4');
fs.mkdirSync(outDir, { recursive: true });

const summary = [];

async function auditRoute(route, formFactor) {
  const url = `${BASE}${route}`;
  const slug =
    (route === '/' ? 'home' : route.replace(/^\//, '').replace(/\//g, '_')) +
    `-${formFactor}`;
  const outPath = path.join(outDir, `${slug}.json`);
  console.log(`Auditing [${formFactor}] ${url}…`);

  let chrome;
  try {
    chrome = await chromeLauncher.launch({
      chromeFlags: ['--headless=new', '--no-sandbox', '--disable-gpu'],
    });

    const config =
      formFactor === 'desktop'
        ? desktopConfig
        : undefined; /* lighthouse default = mobile */

    const result = await lighthouse(
      url,
      {
        port: chrome.port,
        output: 'json',
        logLevel: 'error',
        ...(formFactor === 'mobile'
          ? {
              formFactor: 'mobile',
              screenEmulation: {
                mobile: true,
                width: 360,
                height: 640,
                deviceScaleFactor: 2,
                disabled: false,
              },
            }
          : {}),
      },
      config,
    );
    fs.writeFileSync(outPath, result.report);
    const categories = result.lhr.categories;
    const scores = {
      route,
      formFactor,
      performance: Math.round((categories.performance?.score ?? 0) * 100),
      accessibility: Math.round((categories.accessibility?.score ?? 0) * 100),
      bestPractices: Math.round((categories['best-practices']?.score ?? 0) * 100),
      seo: Math.round((categories.seo?.score ?? 0) * 100),
      seoExempt: SEO_EXEMPT.has(route),
    };
    summary.push(scores);
    console.log(
      `  P ${scores.performance} A ${scores.accessibility} BP ${scores.bestPractices} SEO ${scores.seo}${scores.seoExempt ? ' (SEO exempt)' : ''}`,
    );
  } catch (error) {
    console.error(`  Failed: ${error.message}`);
    summary.push({ route, formFactor, error: true, message: error.message });
  } finally {
    try {
      await chrome?.kill();
    } catch {
      /* Windows temp cleanup can EPERM; report already saved */
    }
  }
}

for (const route of desktopRoutes) {
  await auditRoute(route, 'desktop');
}

for (const route of mobileSpotRoutes) {
  await auditRoute(route, 'mobile');
}

fs.writeFileSync(path.join(outDir, 'summary.json'), JSON.stringify(summary, null, 2));
console.log('\nSummary written to .lighthouseci-r1-4/summary.json');

const failed = summary.filter((row) => {
  if (row.error) return true;
  if (row.accessibility < 100) return true;
  if (row.bestPractices < 100) return true;
  if (row.performance < 95) return true;
  if (!row.seoExempt && row.seo < 100) return true;
  return false;
});

if (failed.length) {
  console.error(`\n${failed.length} route(s) failed R1.4 Lighthouse gates.`);
  process.exit(1);
}

console.log('\nAll R1.4 Lighthouse gates passed.');
