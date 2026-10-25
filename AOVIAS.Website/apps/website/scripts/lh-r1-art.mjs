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

const urls = ['/', '/products', '/products/command'];

const outDir = path.resolve('.lighthouseci-r1-art');
fs.mkdirSync(outDir, { recursive: true });

const summary = [];

for (const route of urls) {
  const url = `${BASE}${route}`;
  const slug = route === '/' ? 'home' : route.replace(/^\//, '').replace(/\//g, '_');
  const outPath = path.join(outDir, `${slug}.json`);
  console.log(`Auditing ${url}…`);

  let chrome;
  try {
    chrome = await chromeLauncher.launch({
      chromeFlags: ['--headless=new', '--no-sandbox', '--disable-gpu'],
    });
    const result = await lighthouse(
      url,
      {
        port: chrome.port,
        output: 'json',
        logLevel: 'error',
      },
      desktopConfig,
    );
    fs.writeFileSync(outPath, result.report);
    const categories = result.lhr.categories;
    const scores = {
      route,
      performance: Math.round((categories.performance?.score ?? 0) * 100),
      accessibility: Math.round((categories.accessibility?.score ?? 0) * 100),
      bestPractices: Math.round((categories['best-practices']?.score ?? 0) * 100),
      seo: Math.round((categories.seo?.score ?? 0) * 100),
    };
    summary.push(scores);
    console.log(
      `  P ${scores.performance} A ${scores.accessibility} BP ${scores.bestPractices} SEO ${scores.seo}`,
    );
  } catch (error) {
    console.error(`  Failed: ${error.message}`);
    summary.push({ route, error: true, message: error.message });
  } finally {
    try {
      await chrome?.kill();
    } catch {
      /* Windows temp cleanup can EPERM; report already saved */
    }
  }
}

fs.writeFileSync(path.join(outDir, 'summary.json'), JSON.stringify(summary, null, 2));
console.log('\nSummary written to .lighthouseci-r1-art/summary.json');

const failed = summary.filter(
  (row) =>
    row.error ||
    row.performance < 95 ||
    row.accessibility < 100 ||
    row.seo < 100,
);
process.exit(failed.length ? 1 : 0);
