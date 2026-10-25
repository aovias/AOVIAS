/**
 * Responsive QA smoke — critical routes across viewport widths.
 * Uses chrome-launcher + CDP screenshots (no Playwright dependency).
 * Requires production server on PORT (default 3000).
 */
import fs from 'node:fs';
import path from 'node:path';
import { createRequire } from 'node:module';

const require = createRequire(import.meta.url);
const chromeLauncher = require('chrome-launcher');

const BASE = process.env.PORT
  ? `http://127.0.0.1:${process.env.PORT}`
  : 'http://127.0.0.1:3000';

const routes = [
  '/',
  '/products',
  '/pricing',
  '/company/contact',
  '/resources',
];

const viewports = [
  { name: '320', width: 320, height: 720 },
  { name: '375', width: 375, height: 812 },
  { name: '414', width: 414, height: 896 },
  { name: '768', width: 768, height: 1024 },
  { name: '820', width: 820, height: 1180 },
  { name: '1024', width: 1024, height: 768 },
  { name: '1280', width: 1280, height: 800 },
  { name: '1440', width: 1440, height: 900 },
  { name: '1600', width: 1600, height: 900 },
  { name: '1920', width: 1920, height: 1080 },
  { name: 'ultrawide', width: 2560, height: 1080 },
];

const outDir = path.resolve('.qa-responsive');
fs.mkdirSync(outDir, { recursive: true });

const results = [];

let chrome;
try {
  chrome = await chromeLauncher.launch({
    chromeFlags: ['--headless=new', '--no-sandbox', '--disable-gpu'],
  });

  // Prefer puppeteer-core if present; else HTTP status matrix only
  let puppeteer;
  try {
    puppeteer = require('puppeteer-core');
  } catch {
    try {
      puppeteer = require('puppeteer');
    } catch {
      puppeteer = null;
    }
  }

  if (!puppeteer) {
    console.warn('puppeteer not installed — running HTTP status matrix only');
    for (const route of routes) {
      for (const vp of viewports) {
        const res = await fetch(`${BASE}${route}`, {
          headers: { 'User-Agent': `AOVIAS-ResponsiveQA/${vp.name}` },
        });
        results.push({
          route,
          viewport: vp.name,
          status: res.status,
          ok: res.status === 200,
          mode: 'http-only',
        });
      }
    }
  } else {
    const browser = await puppeteer.connect({
      browserURL: `http://127.0.0.1:${chrome.port}`,
      defaultViewport: null,
    });
    const page = await browser.newPage();

    for (const route of routes) {
      for (const vp of viewports) {
        await page.setViewport({ width: vp.width, height: vp.height, deviceScaleFactor: 1 });
        const response = await page.goto(`${BASE}${route}`, {
          waitUntil: 'networkidle2',
          timeout: 60000,
        });
        const overflow = await page.evaluate(() => {
          const doc = document.documentElement;
          const body = document.body;
          const scrollWidth = Math.max(doc.scrollWidth, body.scrollWidth);
          const clientWidth = doc.clientWidth;
          return {
            overflowX: scrollWidth > clientWidth + 1,
            scrollWidth,
            clientWidth,
          };
        });
        const slug = `${route === '/' ? 'home' : route.replace(/\//g, '_').slice(1)}-${vp.name}`;
        const shotPath = path.join(outDir, `${slug}.png`);
        await page.screenshot({ path: shotPath, fullPage: false });
        const status = response?.status() ?? 0;
        const statusOk = status === 200 || status === 304;
        const entry = {
          route,
          viewport: vp.name,
          width: vp.width,
          status,
          overflowX: overflow.overflowX,
          ok: statusOk && !overflow.overflowX,
          screenshot: shotPath,
        };
        results.push(entry);
        console.log(
          `${entry.ok ? 'OK' : 'FAIL'} ${route} @ ${vp.name} status=${entry.status} overflowX=${entry.overflowX}`,
        );
      }
    }

    await browser.disconnect();
  }
} finally {
  try {
    await chrome?.kill();
  } catch {
    /* ignore */
  }
}

fs.writeFileSync(path.join(outDir, 'summary.json'), JSON.stringify(results, null, 2));
const failed = results.filter((r) => !r.ok);
console.log(`\nResponsive QA: ${results.length - failed.length}/${results.length} passed`);
console.log(`Summary: ${path.join(outDir, 'summary.json')}`);
process.exit(failed.length ? 1 : 0);
